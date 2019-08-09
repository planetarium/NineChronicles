using System;
using System.Collections.Generic;
using System.Linq;

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

            var lines = csv
                .Trim()
                .Split('\n')
                .Skip(1);
            foreach (var line in lines)
            {
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
