using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Modules.Logging;
using Modules.Json;

namespace RNG_Daemon
{
    /// <summary>
    /// RNG v3.0.0 engine:
    /// - high-quality digit generation (0-9)
    /// - windowed pattern detection
    /// - visible patterns from JSON config
    /// - hidden control pattern resolved from salt+hash
    /// - NDJSON logging for patterns/control/summary
    /// - real-time digit streaming over UNIX socket
    /// </summary>
    public class RngEngine
    {
        public const string Version = "3.0.0";

        private readonly RngConfig _cfg;
        private readonly RandomNumberGenerator _rng;

        private readonly string _sessionId;
        private readonly string _controlPattern;

        private readonly RngSocket _socket;

        private long _sequence = 0;

        public RngEngine(RngConfig cfg)
        {
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            _rng = RandomNumberGenerator.Create();
            _sessionId = GenerateSessionId();
            _controlPattern = ResolveControlPattern(_cfg.ControlSalt, _cfg.ControlHashHex);

            _socket = new RngSocket(_cfg, _controlPattern, _sessionId, Version);
        }

        public void Run()
        {
            Logger.Info("=== RNG Daemon v" + Version + " ===");
            Logger.Info("Device: " + _cfg.DeviceName);
            Logger.Info("Session: " + _sessionId);
            Logger.Info("Window Size: " + _cfg.WindowSize);
            Logger.Info("Patterns: " + string.Join(", ", _cfg.VisiblePatterns));
            Logger.Info("Control pattern: (hidden)");
            Logger.Info("Patterns NDJSON: " + _cfg.PatternsNdjsonPath);
            Logger.Info("Control NDJSON: " + _cfg.ControlNdjsonPath);
            Logger.Info("UNIX socket: " + _cfg.UnixSocketPath);
            Logger.Info("Press Ctrl+C to stop.\n");

            // Start UNIX socket server (non-blocking background thread)
            _socket.Start();

            string window = "";
            int currentMinute = DateTime.Now.Minute;

            // Per-minute tracking
            int samplesThisMinute = 0;
            Dictionary<string, int> minuteHits = InitPatternCounter();
            int controlHitsThisMinute = 0;

            // Cooldown tracking
            Dictionary<string, bool> inWindow = InitPatternPresence();
            bool controlInWindow = false;

            while (true)
            {
                _sequence++;

                // === Generate digit ===
                int digit = GenerateDigit();
                window += digit.ToString();
                if (window.Length > _cfg.WindowSize)
                    window = window.Substring(window.Length - _cfg.WindowSize);

                samplesThisMinute++;

                // === Stream digit in real-time via UNIX socket ===
                _socket.BroadcastDigitEvent(digit, window, _sequence);

                // === Check visible patterns ===
                bool windowHasAnyVisiblePattern = false;
                List<string> foundPatterns = new List<string>();

                foreach (var p in _cfg.VisiblePatterns)
                {
                    bool contains = window.Contains(p);

                    if (contains)
                    {
                        windowHasAnyVisiblePattern = true;
                        foundPatterns.Add(p);

                        if (!inWindow[p])
                        {
                            inWindow[p] = true;
                            minuteHits[p]++;

                            // Log pattern "hit"
                            LogPatternWindow(foundPatterns: new List<string> { p }, window: window);
                        }
                    }
                    else
                    {
                        if (inWindow[p])
                            inWindow[p] = false;
                    }
                }

                // === Check control pattern (blind) ===
                bool controlContains = window.Contains(_controlPattern);

                if (controlContains)
                {
                    if (!controlInWindow)
                    {
                        controlInWindow = true;
                        controlHitsThisMinute++;

                        // Control hit event (no digits/window in payload)
                        LogControlHit();
                    }
                }
                else
                {
                    if (controlInWindow)
                        controlInWindow = false;
                }

                // === Continuous window logging (visible patterns only) ===
                if (windowHasAnyVisiblePattern)
                {
                    LogPatternWindow(foundPatterns, window);
                }

                // === Per-minute summary ===
                int nowMinute = DateTime.Now.Minute;
                if (nowMinute != currentMinute)
                {
                    LogSummary(samplesThisMinute, minuteHits, controlHitsThisMinute);

                    samplesThisMinute = 0;
                    minuteHits = InitPatternCounter();
                    controlHitsThisMinute = 0;
                    currentMinute = nowMinute;
                }

                Thread.Sleep(_cfg.IntervalMs);
            }
        }

        // === Session ID ===
        private static string GenerateSessionId()
        {
            byte[] b = new byte[6]; // 12 hex chars
            RandomNumberGenerator.Fill(b);
            return BitConverter.ToString(b).Replace("-", "").ToLower();
        }

        // === Resolve hidden control pattern ===
        private static string ResolveControlPattern(string salt, string hashHex)
        {
            if (string.IsNullOrWhiteSpace(salt) || string.IsNullOrWhiteSpace(hashHex))
                throw new ArgumentException("Control salt/hash cannot be null or empty.");

            byte[] targetHash = HexToBytes(hashHex);

            using (var sha = SHA256.Create())
            {
                for (int i = 0; i < 1000; i++)
                {
                    string candidate = i.ToString("D3");
                    byte[] data = Encoding.UTF8.GetBytes(salt + candidate);
                    byte[] hash = sha.ComputeHash(data);

                    if (HashesEqual(hash, targetHash))
                        return candidate;
                }
            }

            throw new Exception("Control pattern resolution failed.");
        }

        private static bool HashesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            bool eq = true;
            for (int i = 0; i < a.Length; i++)
                eq &= (a[i] == b[i]);
            return eq;
        }

        private static byte[] HexToBytes(string hex)
        {
            byte[] output = new byte[hex.Length / 2];
            for (int i = 0; i < output.Length; i++)
                output[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return output;
        }

        // === RNG ===
        private int GenerateDigit()
        {
            byte[] b = new byte[4];
            _rng.GetBytes(b);
            int val = BitConverter.ToInt32(b, 0) & 0x7fffffff;
            return val % 10;
        }

        private Dictionary<string, int> InitPatternCounter()
        {
            var d = new Dictionary<string, int>();
            foreach (var p in _cfg.VisiblePatterns)
                d[p] = 0;
            return d;
        }

        private Dictionary<string, bool> InitPatternPresence()
        {
            var d = new Dictionary<string, bool>();
            foreach (var p in _cfg.VisiblePatterns)
                d[p] = false;
            return d;
        }

        // === NDJSON Logging (files for analyzer) ===

        private void LogPatternWindow(List<string> foundPatterns, string window)
        {
            if (foundPatterns == null || foundPatterns.Count == 0)
                return;

            var obj = new
            {
                type = "pattern",
                ts = Timestamp(),
                seq = _sequence,
                window = window,
                patterns = foundPatterns,
                device = _cfg.DeviceName,
                session = _sessionId,
                version = Version
            };

            NdjsonUtils.AppendJsonLine(_cfg.PatternsNdjsonPath, obj);
        }

        private void LogControlHit()
        {
            var obj = new
            {
                type = "control",
                ts = Timestamp(),
                seq = _sequence,
                device = _cfg.DeviceName,
                session = _sessionId,
                version = Version
            };

            NdjsonUtils.AppendJsonLine(_cfg.ControlNdjsonPath, obj);
        }

        private void LogSummary(
            int total,
            Dictionary<string, int> hits,
            int controlHits)
        {
            var hitsObj = new Dictionary<string, int>();
            foreach (var p in _cfg.VisiblePatterns)
            {
                if (hits.TryGetValue(p, out int v))
                    hitsObj[p] = v;
                else
                    hitsObj[p] = 0;
            }

            var obj = new
            {
                type = "summary",
                ts = MinuteTimestamp(),
                total = total,
                hits = hitsObj,
                control = controlHits,
                device = _cfg.DeviceName,
                session = _sessionId,
                version = Version
            };

            NdjsonUtils.AppendJsonLine(_cfg.ControlNdjsonPath, obj);
        }

        // === Timestamp helpers (match v2 style) ===

        //private static string Timestamp()
        //    => DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");

        private static string Timestamp()
              => DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");


        private static string MinuteTimestamp()
        {
            var now = DateTimeOffset.Now;
            var aligned = new DateTimeOffset(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                now.Minute,
                0,
                now.Offset
            );

            return aligned.ToString("yyyy-MM-ddTHH:mm:sszzz");
        }



    }
}
