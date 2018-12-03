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

        public void CreateIndex(string propertyName)
        {
            if (null == Index)
                Index = new Dictionary<string, Dictionary<object, List<string>>>();

            if (!Index.ContainsKey(propertyName))
                Index.Add(propertyName, new Dictionary<object, List<string>>());

            System.Reflection.PropertyInfo prop = typeof(TRow).GetProperty(propertyName);
            if (prop == null)
                return;

            System.Reflection.MethodInfo getMethod = prop.GetGetMethod();
            if (getMethod == null)
                return;

            foreach (var pair in this)
            {
                var val = getMethod.Invoke(pair.Value, null);
                if (null == val)
                    continue;

                if (Index[propertyName].ContainsKey(val) == false)
                    Index[propertyName].Add(val, new List<string>());
                if (Index[propertyName][val].Contains(pair.Key) == false)
                    Index[propertyName][val].Add(pair.Key);
            }
        }

        public List<string> FindIndex(string index, object find)
        {
            if (false == Index.ContainsKey(index))
                return new List<string>();
            return Index[index][find];
        }
    }
}
