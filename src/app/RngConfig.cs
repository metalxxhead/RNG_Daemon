using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Modules.Config;
using Modules.Logging;

namespace RNG_Daemon
{
    /// <summary>
    /// Strongly-typed configuration for the RNG daemon, loaded from configs/rng.json.
    /// </summary>
    public class RngConfig
    {
        public string DeviceName { get; private set; }
        public List<string> VisiblePatterns { get; private set; }
        public string ControlSalt { get; private set; }
        public string ControlHashHex { get; private set; }

        public int WindowSize { get; private set; }
        public int IntervalMs { get; private set; }

        public string PatternsNdjsonPath { get; private set; }
        public string ControlNdjsonPath { get; private set; }

        public string UnixSocketPath { get; private set; }

        public static RngConfig Load(string rootDir)
        {
            string configPath = Path.Combine(rootDir, "configs", "rng.json");

            Logger.Info("Loading RNG JSON config: " + configPath);

            var cfgJson = new ConfigJson();
            if (!cfgJson.Load(configPath))
                throw new Exception("Failed to load RNG JSON config: " + configPath);

            JObject root = cfgJson.Raw();
            if (root == null)
                throw new Exception("rng.json is empty or invalid.");

            var cfg = new RngConfig();

            // Device
            cfg.DeviceName = root["device"]?["name"]?.ToString() ?? "rng_device";

            // Patterns
            var visibleArr = root["patterns"]?["visible"] as JArray;
            cfg.VisiblePatterns = new List<string>();
            if (visibleArr != null)
            {
                foreach (var v in visibleArr)
                {
                    string s = v.ToString();
                    if (!string.IsNullOrWhiteSpace(s))
                        cfg.VisiblePatterns.Add(s);
                }
            }

            if (cfg.VisiblePatterns.Count == 0)
                throw new Exception("No visible patterns defined in rng.json (patterns.visible).");

            var controlObj = root["patterns"]?["control"];
            cfg.ControlSalt = controlObj?["salt"]?.ToString();
            cfg.ControlHashHex = controlObj?["hash"]?.ToString();

            if (string.IsNullOrWhiteSpace(cfg.ControlSalt) || string.IsNullOrWhiteSpace(cfg.ControlHashHex))
                throw new Exception("Control salt/hash not defined in rng.json (patterns.control).");

            // Runtime
            var runtime = root["runtime"];
            cfg.IntervalMs = runtime?["interval_ms"]?.ToObject<int?>() ?? 1000;
            cfg.WindowSize = runtime?["window_size"]?.ToObject<int?>() ?? 10;

            if (cfg.IntervalMs <= 0) cfg.IntervalMs = 1000;
            if (cfg.WindowSize < 3) cfg.WindowSize = 10;

            // Sockets
            var sockets = root["sockets"];
            string unixPath = sockets?["unix_path"]?.ToString() ?? "data/rng.sock";
            cfg.UnixSocketPath = MakeAbsolute(rootDir, unixPath);

            // Logging (NDJSON)
            var logging = root["logging"];
            string patternsPath = logging?["log_ndjson_patterns"]?.ToString() ?? "data/analyzer_stream_patterns.ndjson";
            string controlPath  = logging?["log_ndjson_control"]?.ToString()  ?? "data/analyzer_stream_control.ndjson";

            cfg.PatternsNdjsonPath = MakeAbsolute(rootDir, patternsPath);
            cfg.ControlNdjsonPath  = MakeAbsolute(rootDir, controlPath);

            Logger.Info("RNG Config loaded: " +
                        $"device={cfg.DeviceName}, " +
                        $"windowSize={cfg.WindowSize}, " +
                        $"intervalMs={cfg.IntervalMs}, " +
                        $"socket={cfg.UnixSocketPath}");

            return cfg;
        }

        private static string MakeAbsolute(string rootDir, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return rootDir;

            if (Path.IsPathRooted(path))
                return path;

            return Path.Combine(rootDir, path);
        }
    }
}
