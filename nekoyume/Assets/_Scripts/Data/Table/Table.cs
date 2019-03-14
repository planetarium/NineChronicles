using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace Nekoyume.Data.Table
{
    [Serializable]
    public class Row
    {
    }


    public interface ITable
    {
        void Load(string filename);
    }


    public class Table<TRow> : Dictionary<string, TRow>, ITable where TRow : new()
    {
        public Dictionary<string, Dictionary<object, List<string>>> Index { get; set; }

        public TRow this[int key]
        {
            get
            {
                return this[key.ToString()];
            }
        }

        public bool ContainsKey(int key)
        {
            return ContainsKey(key.ToString());
        }

        public bool TryGetValue(int key, out TRow value)
        {
            return TryGetValue(key.ToString(), out value);
        }

        public void Load(string text)
        {
            var header = new List<string>();

            var lines = text.Split('\n').ToImmutableArray();
            foreach (string line in lines)
            {
                if (lines.IndexOf(line) == 0)
                {
                    header = line.Trim().Split(',').ToList();
                    continue;
                }
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                string[] arr = line.Trim().Split(',');
                TRow row = new TRow();
                int index = 0;
                FieldInfo[] fieldInfos = row.GetType().GetFields();
                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    string key;
                    try
                    {
                        key = header[index].Replace("_", string.Empty);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Debug.Log($"Header not found: {fieldInfo.Name}");
                        continue;
                    }

                    if (!key.Equals(fieldInfo.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"Key not found: {fieldInfo.Name}");
                        continue;
                    }
                    string value = arr[index];
                    Type fieldType = fieldInfo.GetValue(row).GetType();
                    if (fieldType == typeof(int) || fieldType.IsEnum)
                    {
                        if (string.IsNullOrEmpty(value))
                            fieldInfo.SetValue(row, 0);
                        else
                            fieldInfo.SetValue(row, int.Parse(value));
                    }
                    else if (fieldType == typeof(long))
                    {
                        if (string.IsNullOrEmpty(value))
                            fieldInfo.SetValue(row, (long)0);
                        else
                            fieldInfo.SetValue(row, long.Parse(value));
                    }
                    else if (fieldType == typeof(float))
                    {
                        if (string.IsNullOrEmpty(value))
                            fieldInfo.SetValue(row, 0.0f);
                        else
                            fieldInfo.SetValue(row, float.Parse(value));
                    }
                    else if (fieldType == typeof(string))
                    {
                        fieldInfo.SetValue(row, value);
                    }
                    else
                    {
                        continue;
                    }
                    index++;
                }
                Add(arr[0], row);
            }
        }
    }
}
