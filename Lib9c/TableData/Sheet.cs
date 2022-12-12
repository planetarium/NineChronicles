#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using System.Text;
#endif
using Bencodex.Types;
using Nekoyume.Model.State;
using Serilog;
#if UNITY_EDITOR
using Serilog;
#endif

namespace Nekoyume.TableData
{
    [Serializable]
    public abstract class Sheet<TKey, TValue> : IDictionary<TKey, TValue>, ISheet
        where TValue : SheetRow<TKey>, new()
        where TKey : notnull
    {
        private Dictionary<TKey, TValue> _impl;

        private readonly List<int> _invalidColumnIndexes = new List<int>();

        private List<TValue>? _orderedList;

        public string Name { get; }

        public IReadOnlyList<TValue>? OrderedList => _orderedList;

        
        public TValue? First { get; private set; }

        
        public TValue? Last { get; private set; }

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)_impl).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)_impl).Values;

        public int Count => ((IDictionary<TKey, TValue>)_impl).Count;

        public bool IsReadOnly => ((IDictionary<TKey, TValue>)_impl).IsReadOnly;

        public TValue this[TKey key] { get => ((IDictionary<TKey, TValue>)_impl)[key]; set => ((IDictionary<TKey, TValue>)_impl)[key] = value; }

        protected Sheet(string name)
        {
            Name = name;
            _impl = new Dictionary<TKey, TValue>();
        }

        private string? _csv;

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
            _csv = csv;
        }

        public void Set<T>(Sheet<TKey, T> sheet, bool executePostSet = true) where T : TValue, new()
        {
#pragma warning disable LAA1002
            foreach (var sheetRow in sheet)
#pragma warning restore LAA1002
            {
                AddRow(sheetRow.Key, sheetRow);
            }

            if (executePostSet)
            {
                PostSet();
            }
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            if (_orderedList is null)
            {
                throw new InvalidOperationException($"{nameof(_orderedList)} is null.");
            }

            return _orderedList.GetEnumerator();
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value, bool throwException)
        {
            if (_impl.TryGetValue(key, out TValue? v) && !(v is null))
            {
                value = v;
                return true;
            }

            if (throwException)
            {
                throw new SheetRowNotFoundException(Name, key.ToString());
            }

            Log.Debug("{sheetName}: Key - {value}", Name, key.ToString());
            value = default;
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

            _orderedList = Values.OrderBy(value => value.Key).ToList();
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

        public void Add(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)_impl).Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>)_impl).ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>)_impl).Remove(key);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            return ((IDictionary<TKey, TValue>)_impl).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)_impl).Add(item);
        }

        public void Clear()
        {
            ((IDictionary<TKey, TValue>)_impl).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_impl).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)_impl).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)_impl).Remove(item);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>)_impl).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_orderedList is null)
            {
                throw new InvalidOperationException($"{nameof(_orderedList)} is null.");
            }

            return _orderedList.GetEnumerator();
        }

        public IValue Serialize() => _csv.Serialize();
    }
}
