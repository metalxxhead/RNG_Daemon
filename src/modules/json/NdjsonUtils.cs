using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Modules.Json
{
    /// <summary>
    /// NDJSON helper utilities.
    ///
    /// NDJSON = Newline-Delimited JSON:
    ///   { JSON }\n
    ///   { JSON }\n
    ///   ...
    ///
    /// This module provides:
    ///   - AppendJsonLine(): append one JSON object per line
    ///   - ReadLines(): stream lines as raw JSON
    ///   - ReadObjects<T>(): stream deserialized objects
    ///
    /// Perfect for long-running logs or telemetry pipelines.
    /// </summary>
    public static class NdjsonUtils
    {
        /// <summary>
        /// Appends a single object as a JSON line to the target file.
        /// Creates the directory if missing.
        /// </summary>
        public static void AppendJsonLine(string filePath, object obj)
        {
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonConvert.SerializeObject(obj, Formatting.None);

                File.AppendAllText(filePath, json + "\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("NDJSON write error: " + ex.Message);
            }
        }

        /// <summary>
        /// Returns an IEnumerable of raw JSON lines.
        /// Caller can iterate lazily which is ideal for streaming.
        /// </summary>
        public static IEnumerable<string> ReadLines(string filePath)
        {
            if (!File.Exists(filePath))
                yield break;

            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Trim().Length == 0)
                        continue;

                    yield return line;
                }
            }
        }

        /// <summary>
        /// Stream-deserializes NDJSON file into typed objects.
        /// Returns objects one-by-one without loading whole file.
        /// </summary>
        public static IEnumerable<T> ReadObjects<T>(string filePath)
        {
            foreach (var line in ReadLines(filePath))
            {
                T obj = default(T);
                try
                {
                    obj = JsonConvert.DeserializeObject<T>(line);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("NDJSON deserialize error: " + ex.Message);
                }

                if (obj != null)
                    yield return obj;
            }
        }
    }
}

