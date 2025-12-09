using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Modules.Logging;
using Modules.Json;

namespace RNG_Daemon
{
    /// <summary>
    /// Simple UNIX domain socket broadcaster for RNG events.
    /// - On client connect: send meta event (device/session/version/patterns/controlPattern).
    /// - Each digit: broadcast NDJSON "digit" event.
    /// </summary>
    public class RngSocket
    {
        private readonly RngConfig _cfg;
        private readonly string _controlPattern;
        private readonly string _sessionId;
        private readonly string _version;

        private Socket _listener;
        private readonly List<NetworkStream> _clients = new List<NetworkStream>();
        private readonly object _clientsLock = new object();

        private bool _started = false;

        public RngSocket(RngConfig cfg, string controlPattern, string sessionId, string version)
        {
            _cfg = cfg;
            _controlPattern = controlPattern;
            _sessionId = sessionId;
            _version = version;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;

            var thread = new Thread(ServerLoop)
            {
                IsBackground = true,
                Name = "RngSocketServer"
            };
            thread.Start();
        }

        private void ServerLoop()
        {
            try
            {
                // Ensure socket directory exists
                string dir = Path.GetDirectoryName(_cfg.UnixSocketPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Remove stale socket file
                if (File.Exists(_cfg.UnixSocketPath))
                {
                    try { File.Delete(_cfg.UnixSocketPath); }
                    catch (Exception ex)
                    {
                        Logger.Warn("Could not delete old socket file: " + ex.Message);
                    }
                }

                _listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                var endPoint = new UnixDomainSocketEndPoint(_cfg.UnixSocketPath);
                _listener.Bind(endPoint);
                _listener.Listen(5);

                Logger.Info("RNG UNIX socket listening on " + _cfg.UnixSocketPath);

                while (true)
                {
                    Socket client = _listener.Accept();

                    var t = new Thread(HandleClient)
                    {
                        IsBackground = true,
                        Name = "RngSocketClient"
                    };
                    t.Start(client);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RNG socket server error: " + ex.Message);
            }
        }

        private void HandleClient(object state)
        {
            Socket sock = (Socket)state;
            NetworkStream stream = null;

            try
            {
                stream = new NetworkStream(sock, ownsSocket: true);

                lock (_clientsLock)
                {
                    _clients.Add(stream);
                }

                Logger.Info("RNG socket client connected.");

                // Send meta handshake, including hidden control pattern
                var metaObj = new
                {
                    type = "meta",
                    ts = Timestamp(),
                    device = _cfg.DeviceName,
                    session = _sessionId,
                    version = _version,
                    visible_patterns = _cfg.VisiblePatterns,
                    control_pattern = _controlPattern
                };

                string metaLine = JsonUtils.Serialize(metaObj, pretty: false);
                WriteLine(stream, metaLine);

                // We don't expect any data from the client; just block until disconnect
                byte[] buffer = new byte[1];
                while (true)
                {
                    int read = 0;
                    try
                    {
                        read = stream.Read(buffer, 0, buffer.Length);
                    }
                    catch
                    {
                        break;
                    }

                    if (read <= 0)
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("RNG socket client error: " + ex.Message);
            }
            finally
            {
                if (stream != null)
                {
                    lock (_clientsLock)
                    {
                        _clients.Remove(stream);
                    }
                    try { stream.Dispose(); } catch { }
                }

                Logger.Info("RNG socket client disconnected.");
            }
        }

        /// <summary>
        /// Called from RngEngine on every generated digit.
        /// Broadcasts a NDJSON "digit" event to all connected clients.
        /// </summary>
        public void BroadcastDigitEvent(int digit, string window, long seq)
        {
            if (!_started) return;

            var obj = new
            {
                type = "digit",
                ts = Timestamp(),
                seq = seq,
                digit = digit,
                window = window,
                device = _cfg.DeviceName,
                session = _sessionId,
                version = _version
            };

            string line = JsonUtils.Serialize(obj, pretty: false);
            BroadcastLine(line);
        }

        private void BroadcastLine(string line)
        {
            lock (_clientsLock)
            {
                if (_clients.Count == 0)
                    return;

                List<NetworkStream> dead = null;

                foreach (var client in _clients)
                {
                    try
                    {
                        WriteLine(client, line);
                    }
                    catch
                    {
                        if (dead == null) dead = new List<NetworkStream>();
                        dead.Add(client);
                    }
                }

                if (dead != null)
                {
                    foreach (var d in dead)
                    {
                        _clients.Remove(d);
                        try { d.Dispose(); } catch { }
                    }
                }
            }
        }

        private static void WriteLine(NetworkStream stream, string line)
        {
            if (stream == null) return;

            byte[] data = Encoding.UTF8.GetBytes(line + "\n");
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        private static string Timestamp()
            => DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
    }
}
