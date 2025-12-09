using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Modules.Config
{
    /// <summary>
    /// JSON configuration loader for CLI apps.
    /// Structure supports:
    /// {
    ///   "app": { "debug": true, "timezone": "America/Chicago" },
    ///   "database": { "path": "data/db.sqlite" }
    /// }
    ///
    /// This mirrors ConfigIni but works with JSON instead.
    /// </summary>
    public class ConfigJson
    {
        private JObject root;

        public bool Load(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("JSON config not found: " + path);
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                root = JObject.Parse(json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("JSON config parse error: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Safely retrieves a JToken using section/key.
        /// </summary>
        private JToken GetToken(string section, string key)
        {
            if (root == null)
                return null;

            var sec = root[section];
            if (sec == null)
                return null;

            var tok = sec[key];
            return tok;
        }

        // -----------------------------
        // Typed Getters
        // -----------------------------

        public string GetString(string section, string key, string defaultValue = null)
        {
            var t = GetToken(section, key);
            return (t != null) ? t.ToString() : defaultValue;
        }

        public int GetInt(string section, string key, int defaultValue = 0)
        {
            var t = GetToken(section, key);
            if (t == null) return defaultValue;

            int v;
            return int.TryParse(t.ToString(), out v) ? v : defaultValue;
        }

        public bool GetBool(string section, string key, bool defaultValue = false)
        {
            var t = GetToken(section, key);
            if (t == null) return defaultValue;

            string s = t.ToString().ToLower();

            if (s == "true" || s == "1" || s == "yes" || s == "on")
                return true;

            if (s == "false" || s == "0" || s == "no" || s == "off")
                return false;

            return defaultValue;
        }

        /// <summary>
        /// Raw JObject in case you want advanced access.
        /// </summary>
        public JObject Raw()
        {
            return root;
        }
    }
}
