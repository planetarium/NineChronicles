using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Nekoyume.TableData
{
    [Serializable]
    public abstract class Sheet<TKey, TValue> : Dictionary<TKey, TValue>
        where TValue : SheetRow<TKey>, new()
    {
        private readonly List<int> _invalidColumnIndexes = new List<int>();

        private IOrderedEnumerable<TValue> _enumerable;
        private List<TValue> _orderedList;
        private TValue _first;
        private TValue _last;

        public IReadOnlyList<TValue> OrderedList => _orderedList;
        public TValue First => _first;
        public TValue Last => _last;

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

                if (!TryGetRow(line, out var row))
                {
                    // Do not throw any exceptions for check all of lines.
                    continue;
                }

                AddRow(row.Key, row);
            }

            PostSet();
        }

        public void Set<T>(Sheet<TKey, T> sheet, bool executePostSet = true) where T : TValue, new()
        {
            foreach (var sheetRow in sheet)
            {
                AddRow(sheetRow.Key, sheetRow);
            }

            if (executePostSet)
            {
                PostSet();
            }
        }

        public new IEnumerator<TValue> GetEnumerator()
        {
            return _enumerable.GetEnumerator();
        }

        protected virtual void AddRow(TKey key, TValue value)
        {
            Add(key, value);
        }

        private void PostSet()
        {
            foreach (var value in Values)
            {
                value.EndOfSheetInitialize();
            }

            _enumerable = Values.OrderBy(value => value.Key);
            _orderedList = _enumerable.ToList();
            _first = _orderedList.First();
            _last = _orderedList.Last();
        }

        private bool TryGetRow(string csv, out TValue row)
        {
            var fields = csv.Trim().Split(',')
                .Where((column, index) => !_invalidColumnIndexes.Contains(index))
                .ToArray();
            row = new TValue();
            row.Set(fields);

#if UNITY_EDITOR
            try
            {
                row.Validate();
            }
            catch (SheetRowValidateException e)
            {
                var sb = new StringBuilder();
                sb.AppendLine(GetType().Name);
                sb.AppendLine(row.Key.ToString());
                sb.AppendLine(e.Message);
                Debug.LogError(sb.ToString());

                return false;
            }
#endif

            return true;
        }
    }
}
