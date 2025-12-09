using System;
using System.IO;
using System.Collections.Generic;
using Modules.Logging;
using Modules.Config;
using Modules.Json;

namespace RNG_Daemon
{
    /// <summary>
    /// Core runtime orchestrator for CLI apps.
    /// This class wires together:
    ///   - argument parsing
    ///   - config loading
    ///   - logging initialization
    ///   - optional modules (json, ndjson, sqlite)
    ///   - the actual program workflow
    ///
    /// The goal is to give you a unified place where all runtime setup lives.
    /// </summary>
    public class Core
    {
        public Dictionary<string, string> Args { get; private set; }

        /// <summary>
        /// Root directory of the app (parent of bin/).
        /// Example: /home/damien/RNG_Daemon
        /// </summary>
        public string RootDir { get; private set; }

        public Core() { }

        /// <summary>
        /// Main entry point used by Program.cs.
        /// It receives the raw CLI args, prepares the environment,
        /// and then executes your application logic.
        /// </summary>
        public void Run(string[] rawArgs)
        {
            // -----------------------------
            // 1. Argument Parsing
            // -----------------------------
            //var parser = new ArgumentParser()
            //    .Define("debug", "Enable debug logging (true/false)", defaultValue: "false")
            //    .Define("config", "Path to INI config file", defaultValue: "configs/app.ini");

            //Args = parser.Parse(rawArgs);

            // -----------------------------
            // 2. Logging Setup
            // -----------------------------
            //bool debugMode = Args.ContainsKey("debug") && Args["debug"].ToLower() == "true";

            // Always allow console logging
            Logger.Info("Initializing application...");

            //if (debugMode)
            //    Logger.Info("Debug mode enabled.");

            // -----------------------------
            // 3. Load INI Config File
            // -----------------------------

            // ---------------------------------------------------------
            // Resolve config path reliably relative to the executable
            // ---------------------------------------------------------
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDir = Path.GetDirectoryName(exePath);
            string rootDir = Directory.GetParent(exeDir).FullName;
            RootDir = rootDir;

            //string configRelative = Args["config"];
            //string configPath = Path.Combine(rootDir, configRelative);

            Logger.Info("ExePath = " + exePath);
            Logger.Info("ExeDir = " + exeDir);
            Logger.Info("RootDir = " + rootDir);
            //Logger.Info("IniConfigPath = " + configPath);


            // -----------------------------
            // 4. Optional: Enable file logging
            // -----------------------------
                Logger.EnableFileLogging(Path.Combine(rootDir, "logs/app.log"));
                Logger.Info("File logging enabled");
            


            // -----------------------------
            // 5. Application Workflow
            // -----------------------------
            Logger.Info("Running application logic (RNG v" + RngEngine.Version + ")...");

            ApplicationLogic();

            Logger.Info("Application finished.");
        }

        /// <summary>
        /// This is where you place the actual logic of your program.
        /// For RNG v3.0.0, we bootstrap the RNG engine.
        /// </summary>
        private void ApplicationLogic()
        {
            try
            {
                // Load RNG JSON config
                var rngConfig = RngConfig.Load(RootDir);

                // Start RNG engine (blocking loop)
                var engine = new RngEngine(rngConfig);
                engine.Run();
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal error in ApplicationLogic: " + ex.Message);
            }
        }
    }
}
