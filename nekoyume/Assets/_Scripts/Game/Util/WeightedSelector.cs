using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.Game.Util
{
    public class WeightedSelector<T>
    {
        private List<T> _values;
        private List<int> _weights;
        private int _totalWeights;

        public int Count
        {
            get
            {
                return _values.Count;
            }
        }

        public WeightedSelector()
        {
            _values = new List<T>();
            _weights = new List<int>();
            _totalWeights = 0;
        }

        public void Add(T item, int weight)
        {
            _values.Add(item);
            _weights.Add(weight);
            _totalWeights += weight;
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

            var random = new System.Random();
            int rnd = random.Next(0, sum);
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

            return _values[idx];
        }

        public T Pop()
        {
            return Select(true);
        }
    }
}
