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
                    header = header.ConvertAll(k => k.ToLower().Replace("_", string.Empty));
                    continue;
                }
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                string[] arr = line.Trim().Split(',');
                TRow row = new TRow();
                FieldInfo[] fieldInfos = row.GetType().GetFields();
                foreach (FieldInfo fieldInfo in fieldInfos)
                {
                    var fieldName = fieldInfo.Name;
                    int index = header.FindIndex(i => i == fieldName.ToLower());
                    if (index == -1)
                    {
                        continue;
                    }

                    string value = String.Empty;
                    try
                    {
                        value = arr[index];

                    }
                    catch (IndexOutOfRangeException)
                    {
                        Debug.LogError($"Invalid Table: {GetType()}");
                        throw;
                    }
                    // 필드 기본값이 설정되지 않으면 NullReferenceException이 발생함.
                    Type fieldType;
                    try
                    {
                        fieldType = fieldInfo.GetValue(row).GetType();
                    }
                    catch (NullReferenceException)
                    {
                        Debug.Log($"Set {value} first.");
                        continue;
                    }
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
                    else if (fieldType == typeof(bool))
                    {
                        fieldInfo.SetValue(row, int.Parse(value) == 1);
                    }
                    else if (fieldType == typeof(decimal))
                    {
                        fieldInfo.SetValue(row, string.IsNullOrEmpty(value) ? 0m : decimal.Parse(value));
                    }
                    else
                    {
                        continue;
                    }
                    index++;
                }

                try
                {
                    Add(arr[0], row);
                }
                catch (ArgumentException)
                {
                    Debug.LogError(arr[0]);
                    throw;
                }
            }
        }
    }
}
