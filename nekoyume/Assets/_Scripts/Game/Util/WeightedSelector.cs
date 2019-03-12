using System.Collections.Generic;
using Libplanet.Action;

namespace Nekoyume.Game.Util
{
    public class WeightedSelector<T>
    {
        private readonly List<T> _values;
        private readonly List<int> _weights;
        private readonly IRandom _random;

        public int Count => _values.Count;

        public WeightedSelector(IRandom seed)
        {
            _values = new List<T>();
            _weights = new List<int>();
            _random = seed;
        }

        public void Add(T item, int weight)
        {
            _values.Add(item);
            _weights.Add(weight);
        }

        public T Select(bool pop = false)
        {
            int i = 0;
            int sum = 0;
            int len = _weights.Count;
            for (i = 0; i < len; ++i)
            {
                sum += _weights[i];
            }

            int rnd = _random.Next(0, sum);
            int idx = -1;
            for (i = 0; i < len; ++i)
            {
                if (rnd < _weights[i])
                {
                    idx = i;
                    break;
                }
                rnd -= _weights[i];
            }

            if (pop)
            {
                _values.RemoveAt(idx);
                _weights.RemoveAt(idx);
            }
            if (idx >= 0)
                return _values[idx];
            return default(T);
        }

        public T Pop()
        {
            return Select(true);
        }
    }
}
