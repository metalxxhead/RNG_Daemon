using System;
using System.IO;

namespace Modules.Logging
{
    /// <summary>
    /// Simple logging utility for CLI apps.
    /// Supports console output and optional file logging.
    /// </summary>
    public static class Logger
    {
        private static readonly object FileLock = new object();

        private static bool fileLoggingEnabled = false;
        private static string logFilePath = "logs/app.log";

        /// <summary>
        /// Enable writing logs to a file. Creates logs/ if needed.
        /// </summary>
        public static void EnableFileLogging(string path = null)
        {
            fileLoggingEnabled = true;

            if (path != null)
                logFilePath = path;

            string dir = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public static void Info(string message)
        {
            Write("INFO", message, ConsoleColor.Gray);
        }

        public static void Warn(string message)
        {
            Write("WARN", message, ConsoleColor.Yellow);
        }

        public static void Error(string message)
        {
            Write("ERROR", message, ConsoleColor.Red);
        }

        /// <summary>
        /// Core log function writing to console and optionally to file.
        /// </summary>
        private static void Write(string level, string message, ConsoleColor color)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string line = $"[{timestamp}] {level}: {message}";

            // Console output
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ResetColor();

            // Optional file output
            if (fileLoggingEnabled)
            {
                lock (FileLock)
                {
                    try
                    {
                        File.AppendAllText(logFilePath, line + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Logging error: " + ex.Message);
                    }
                }
            }
        }
    }
}

