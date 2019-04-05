using System.Collections.Generic;
using Libplanet.Action;

namespace Nekoyume.Game.Util
{
    public class WeightedSelector<T>
    {
        private readonly List<T> _values;
        private readonly List<float> _weights;
        private readonly IRandom _random;

        public int Count => _values.Count;

        public WeightedSelector(IRandom random)
        {
            _values = new List<T>();
            _weights = new List<float>();
            _random = random;
        }

        public void Add(T item, float weight)
        {
            _values.Add(item);
            _weights.Add(weight);
        }

        public T Select(bool pop = false)
        {
            int i = 0;
            int len = _weights.Count;

            float rnd = (float) _random.NextDouble();
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
