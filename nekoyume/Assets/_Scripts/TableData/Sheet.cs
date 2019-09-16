using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nekoyume.TableData
{
    [Serializable]
    public abstract class Sheet<TKey, TValue> : Dictionary<TKey, TValue>, IEnumerable<TValue>
        where TValue : SheetRow<TKey>, new()
    {
        private readonly List<int> _invalidColumnIndexes = new List<int>();
        private IOrderedEnumerable<TValue> _enumerable;
        private List<TValue> _orderedList;

        public void Set(string csv)
        {
            if (string.IsNullOrEmpty(csv))
            {
                throw new ArgumentNullException(nameof(csv));
            }

            var lines = csv
                .Trim()
                .Split('\n');
            if (lines.Length == 0)
            {
                throw new InvalidDataException(nameof(csv));
            }

            var columnNames = lines[0].Trim().Split(',');
            for (var i = 0; i < columnNames.Length; i++)
            {
                var columnName = columnNames[i];
                if (columnName.StartsWith("_"))
                {
                    _invalidColumnIndexes.Add(i);
                }
            }

            var linesWithoutColumnName = lines.Skip(1);
            foreach (var line in linesWithoutColumnName)
            {
                if (line.StartsWith("_"))
                {
                    continue;
                }

                var row = CSVToRow(line);
                Add(row.Key, row);
            }

            _enumerable = Values.OrderBy(value => value.Key);
            _orderedList = _enumerable.ToList();
        }

        public new IEnumerator<TValue> GetEnumerator()
        {
            return _enumerable.GetEnumerator();
        }

        public List<TValue> ToOrderedList()
        {
            return _orderedList;
        }

        private TValue CSVToRow(string csv)
        {
            var fields = csv.Trim().Split(',')
                .Where((column, index) => !_invalidColumnIndexes.Contains(index))
                .ToList();
            var row = new TValue();
            row.Set(fields);
            return row;
        }
    }
}
