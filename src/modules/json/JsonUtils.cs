using System;
using Newtonsoft.Json;

namespace Modules.Json
{
    /// <summary>
    /// Simple JSON (de)serialization helpers using Newtonsoft.Json.
    /// This module is optional; delete folder if not needed.
    /// </summary>
    public static class JsonUtils
    {
        /// <summary>
        /// Serialize an object to JSON text.
        /// Pretty print by default but can be set compact.
        /// </summary>
        public static string Serialize(object obj, bool pretty = true)
        {
            try
            {
                return JsonConvert.SerializeObject(
                    obj,
                    pretty ? Formatting.Indented : Formatting.None
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("JSON serialize error: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Deserialize JSON text to a typed object.
        /// Returns default(T) on errors.
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("JSON deserialize error: " + ex.Message);
                return default(T);
            }
        }

        /// <summary>
        /// Safe deserialization for unknown types.
        /// Returns null on error.
        /// </summary>
        public static object Deserialize(string json, Type t)
        {
            try
            {
                return JsonConvert.DeserializeObject(json, t);
            }
            catch (Exception ex)
            {
                Console.WriteLine("JSON deserialize error: " + ex.Message);
                return null;
            }
        }
    }
}

