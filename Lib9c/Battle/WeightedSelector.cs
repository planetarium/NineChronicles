using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Serilog;

namespace Nekoyume.Battle
{
    public class WeightedSelector<T>
    {
        private readonly struct Item
        {
            public readonly T Value;
            public readonly decimal Weight;

            public Item(T item, decimal weight)
            {
                Value = item;
                Weight = weight;
            }
        }

        private readonly IRandom _random;
        private readonly List<Item> _items;

        public int Count => _items.Count;

        public WeightedSelector(IRandom random)
        {
            _random = random;
            _items = new List<Item>();
        }

        public void Add(T item, decimal weight)
        {
            if (weight > 0)
            {
                _items.Add(new Item(item, weight));
            }
            else
            {
                Log.Warning($"weight must be greater than 0.");
            }
        }

        public IEnumerable<T> Select(int count)
        {
            Validate(count);
            var result = new List<T>();
            var weight = 0m;
            var rnd = _random.Next(1, 100001) * 0.00001m;
            var sum = _items.Sum(i => i.Weight);
            var ratio = 1.0m / sum;
            while (result.Count < count)
            {
                foreach (var item in _items.OrderBy(i => i.Weight).ToList())
                {
                    weight += (item.Weight * ratio);

                    if (rnd <= weight)
                    {
                        result.Add(item.Value);
                        _items.Remove(item);
                        weight = 0m;
                    }

                    if (result.Count == count)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        [Obsolete("Use Select")]
        public IEnumerable<T> SelectV1(int count)
        {
            Validate(count);
            var result = new List<T>();
            var weight = 0m;
            var rnd = _random.Next(1, 100001) * 0.00001m;
            while (result.Count < count)
            {
                foreach (var item in _items.OrderBy(i => i.Weight).ToList())
                {
                    weight += item.Weight;

                    if (rnd <= weight)
                    {
                        result.Add(item.Value);
                        _items.Remove(item);
                    }

                    if (result.Count == count)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        [Obsolete("Use Select")]
        public IEnumerable<T> SelectV2(int count)
        {
            Validate(count);
            var result = new List<T>();
            var weight = 0m;
            var rnd = _random.Next(1, 100001) * 0.00001m;
            while (result.Count < count)
            {
                foreach (var item in _items.OrderBy(i => i.Weight).ToList())
                {
                    weight += item.Weight;

                    if (rnd <= weight)
                    {
                        result.Add(item.Value);
                        _items.Remove(item);
                        weight = 0m;
                    }

                    if (result.Count == count)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        private void Validate(int count)
        {
            if (count <= 0)
            {
                throw new InvalidCountException();
            }

            if (_items.Count <= 0)
            {
                throw new ListEmptyException();
            }
        }
    }

    public class InvalidCountException : InvalidOperationException
    {
        public InvalidCountException() : base("count must be greater than 0.")
        {
        }
    }

    public class ListEmptyException : InvalidOperationException
    {
        public ListEmptyException(string message = "list is empty. add value first") : base(message)
        {
        }
    }
}
