using System;
using System.Collections.Specialized;
using System.IO;

namespace OneStore.Common
{
    public class PropertiesFile
    {
        private readonly NameValueCollection properties;

        public PropertiesFile(string path)
        {
            properties = new NameValueCollection();

            foreach (var row in File.ReadAllLines(path))
            {
                if (!string.IsNullOrEmpty(row) && !row.StartsWith("#"))
                {
                    var idx = row.IndexOf('=');
                    if (idx > 0)
                        properties.Add(row.Substring(0, idx).Trim(), row.Substring(idx + 1).Trim());
                }
            }
        }

        public string GetProperty(string key) => properties[key];
        
    }
}