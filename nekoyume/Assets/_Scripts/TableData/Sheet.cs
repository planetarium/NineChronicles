using System;
using System.Collections.Generic;

namespace Nekoyume.TableData
{
    public interface ISheetRow<out T>
    {
        T Key { get; }
        void Set(string[] fields);
    }

    [Serializable]
    public abstract class Sheet<TKey, TValue> : Dictionary<TKey, TValue> where TValue : ISheetRow<TKey>, new()
    {
        public void Set(string csv)
        {
            if (string.IsNullOrEmpty(csv))
            {
                throw new ArgumentNullException(nameof(csv));
            }

            var isFirstLine = true;
            var lines = csv.Trim().Split('\n');
            foreach (var line in lines)
            {
                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }
                
                var row = CSVToRow(line);
                Add(row.Key, row);
            }
        }
        
        private TValue CSVToRow(string csv)
        {
            var fields = csv.Trim().Split(',');
            var row = new TValue();
            row.Set(fields);
            return row;
        }
    }
}
