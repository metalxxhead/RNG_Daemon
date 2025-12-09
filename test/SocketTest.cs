using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class SocketTest
{
    // Compiles with:
    // mcs -r:System.Net.dll -r:System.Core.dll -r:Newtonsoft.Json.dll SocketTest.cs
    // Requires Newtonsoft.Json.dll to be in the same directory as the compiled binary


    // The control pattern is stored privately and never printed
    private static string _controlPattern = null;

    static void Main(string[] args)
    {
        string socketPath = args.Length > 0 ? args[0] : "../data/rng.sock";

        Console.WriteLine("Connecting to RNG socket: " + socketPath);

        try
        {
            using (var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
            {
                var endpoint = new UnixDomainSocketEndPoint(socketPath);
                socket.Connect(endpoint);

                using (var stream = new NetworkStream(socket))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    Console.WriteLine("Connected. Listening for NDJSON events...\n");

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        HandleEvent(line);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Socket error: " + ex.Message);
        }
    }

    private static void HandleEvent(string jsonLine)
    {
        if (string.IsNullOrWhiteSpace(jsonLine))
            return;

        JObject obj;
        try
        {
            obj = JObject.Parse(jsonLine);
        }
        catch
        {
            Console.WriteLine("Invalid JSON: " + jsonLine);
            return;
        }

        string type = (string)obj["type"];

        switch (type)
        {
            // ============================================================
            // META EVENT (contains control_pattern — do NOT print it)
            // ============================================================
            case "meta":
                Console.WriteLine("[meta] Connected to RNG source");
                Console.WriteLine("       device=" + (string)obj["device"]);
                Console.WriteLine("       session=" + (string)obj["session"]);
                Console.WriteLine("       version=" + (string)obj["version"]);

                // Store control pattern silently (NEVER print)
                _controlPattern = (string)obj["control_pattern"];
                // Console.WriteLine("DEBUG: stored control pattern internally"); // DEBUG only, do not enable
                break;

            // ============================================================
            // DIGIT EVENT
            // ============================================================
            case "digit":
                Console.WriteLine(
                    $"[digit] seq={obj["seq"]} digit={obj["digit"]} window={obj["window"]}"
                );
                break;

            // ============================================================
            // VISIBLE PATTERN EVENT
            // ============================================================
            case "pattern":
                string pats = string.Join(",", obj["patterns"]);
                Console.WriteLine(
                    $"[pattern] seq={obj["seq"]} patterns={pats} window={obj["window"]}"
                );
                break;

            // ============================================================
            // CONTROL EVENT (do NOT expose the control pattern)
            // ============================================================
            case "control":
                Console.WriteLine($"[control] seq={obj["seq"]} (hidden pattern detected)");
                break;

            // ============================================================
            // SUMMARY EVENT
            // ============================================================
            case "summary":
                Console.WriteLine($"[summary] total={obj["total"]} control_hits={obj["control"]}");
                break;

            default:
                Console.WriteLine("[unknown] " + jsonLine);
                break;
        }
    }
}
