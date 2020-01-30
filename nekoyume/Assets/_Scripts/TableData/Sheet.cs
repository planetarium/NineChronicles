using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Serilog;

namespace Nekoyume.TableData
{
    [Serializable]
    public abstract class Sheet<TKey, TValue> : Dictionary<TKey, TValue>
        where TValue : SheetRow<TKey>, new()
    {
        private readonly List<int> _invalidColumnIndexes = new List<int>();

        private IOrderedEnumerable<TValue> _enumerable;
        private List<TValue> _orderedList;

        public string Name { get; }
        public IReadOnlyList<TValue> OrderedList => _orderedList;
        [CanBeNull]
        public TValue First { get; private set; }
        [CanBeNull]
        public TValue Last { get; private set; }

        protected Sheet(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="csv"></param>
        /// <param name="isReversed">true: csv값의 column과 row가 뒤집혀서 작성되어 있다고 판단합니다.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        public void Set(string csv, bool isReversed = false)
        {
            if (string.IsNullOrEmpty(csv))
            {
                throw new ArgumentNullException(nameof(csv));
            }

            // todo: 리버스 로직 작성 필요.
            if (isReversed)
            {
                return;
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
                if (line.StartsWith(",") ||
                    line.StartsWith("_"))
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

        public bool TryGetValue(TKey key, out TValue value, bool throwException = false)
        {
            if (base.TryGetValue(key, out value))
                return true;

            if (throwException)
                throw new SheetRowNotFoundException(Name, key.ToString());

            return false;
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
            First = _orderedList.FirstOrDefault();
            Last = _orderedList.LastOrDefault();
        }

        private bool TryGetRow(string line, out TValue row)
        {
            var fields = line.Trim().Split(',')
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
                Log.Error(e, sb.ToString());

                return false;
            }
#endif

            return true;
        }
    }
}
