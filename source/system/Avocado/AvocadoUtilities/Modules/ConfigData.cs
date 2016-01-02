﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AvocadoUtilities.Modules
{
    public class ConfigData
    {
        const string DELIM = "=";

        Dictionary<string, string> cache;

        public async Task<string> GetValue(
            string property,
            string defaultVal)
        {
            // Initialize cache, if needed.
            cache = cache ?? await getPropertyDict();

            // Check if the value is already cached.
            if (cache.ContainsKey(property)) return cache[property];

            // Otherwise, the config file did not exist, or the property was
            // not found. Create a new file if needed and add the property with
            // its default value.
            using (var writer = new StreamWriter(getConfigFile(), true))
            {
                await writer.WriteLineAsync($"{property}{DELIM}{defaultVal}");
            }

            // Cache the property/value for fast subsequent access.
            cache[property] = defaultVal;

            return defaultVal;
        }

        string getConfigFile() 
            => Path.Combine(RootDir.Avocado.Apps.MyAppDataPath, "config.ini");

        async Task<Dictionary<string, string>> getPropertyDict()
        {
            var ret = new Dictionary<string, string>();
            var configFile = getConfigFile();

            if (!File.Exists(configFile))
            {
                return ret;
            }

            string fileContents;
            using (var reader = new StreamReader(configFile))
            {
                fileContents = await reader.ReadToEndAsync();
            }

            var lines = fileContents.Split(
                new string[] { Environment.NewLine },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var index = line.IndexOf(DELIM);
                if (index == -1) continue;

                var property = line.Substring(0, index);
                if (string.IsNullOrWhiteSpace(property)) continue;

                var val = line.Substring(index + DELIM.Length);
                if (string.IsNullOrWhiteSpace(val)) continue;

                ret[property] = val;
            }

            return ret;
        }
    }
}