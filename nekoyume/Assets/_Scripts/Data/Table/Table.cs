using System;
using System.Collections.Generic;
using System.Reflection;


namespace Nekoyume.Data.Table
{
    public class Row
    {
    }


    public interface ITable
    {
        void Load(string filename);
    }


    public class Table<TRow> : Dictionary<string, TRow>, ITable where TRow : new()
    {
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
            bool header = false;
            string[] lines = text.Split('\n');
            foreach (string line in lines)
            {
                if (!header)
                {
                    header = true;
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
                    string value = arr[index];
                    Type fieldType = fieldInfo.GetValue(row).GetType();
                    if (fieldType == typeof(int))
                    {
                        fieldInfo.SetValue(row, int.Parse(value));
                    }
                    else if (fieldType == typeof(long))
                    {
                        fieldInfo.SetValue(row, long.Parse(value));
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
