using System.Collections.Generic;
using Libplanet.Action;

namespace Nekoyume.Battle
{
    public class WeightedSelector<T>
    {
        private readonly List<T> _values;
        private readonly List<decimal> _weights;
        private readonly IRandom _random;

        public int Count => _values.Count;

        public WeightedSelector(IRandom random)
        {
            _values = new List<T>();
            _weights = new List<decimal>();
            _random = random;
        }

        public void Add(T item, decimal weight)
        {
            _values.Add(item);
            _weights.Add(weight);
        }

        private T Select(bool pop = false)
        {
            var i = 0;
            var len = _weights.Count;

            var rnd = _random.Next(0, 100000) * 0.00001m;
            var idx = -1;
            for (i = 0; i < len; ++i)
            {
                if (rnd < _weights[i])
                {
                    idx = i;
                    break;
                }
                rnd -= _weights[i];
            }

            var value = _values[i];

            if (pop)
            {
                _values.RemoveAt(idx);
                _weights.RemoveAt(idx);
            }

            return value;
        }

        public T Pop()
        {
            return Select(true);
        }
    }
}
