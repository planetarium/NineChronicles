using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace Nekoyume
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Sample<T>(this IEnumerable<T> population, int k)
        {
            // Ported from CPython's standard library: Lib/random.py (v3.8.0) line 315-384
            var pool = population.ToArray();
            var n = pool.Length;
            if (k > n)
            {
                throw new ArgumentException("Cannot sample more than the population.", nameof(k));
            }
            else if (k <= 0)
            {
                throw new ArgumentException("Less than zero cannot be sampled.", nameof(k));
            }

            var setSize = 21; // size of a small set minus size of an empty list
            if (k > 5)
            {
                setSize += Pow(4, (int)Math.Ceiling(Math.Log(k * 3, 4)));
            }

            var random = new Random();
            if (n <= setSize)
            {
                // An n-length list is smaller than a k-length set
                for (var i = 0; i < k; i++)
                {
                    var j = random.Next(0, n - i);
                    yield return pool[j];
                    pool[j] = pool[n - i - 1]; // move non-selected item into vacancy
                }
            }
            else
            {
                var selected = new HashSet<int>();
                for (var i = 0; i < k; i++)
                {
                    int j;
                    do
                    {
                        j = random.Next(0, n - i);
                    } while (selected.Contains(j));

                    selected.Add(j);
                    yield return pool[j];
                }
            }
        }

        private static int Pow(int v, int power)
        {
            var rv = 1;
            while (power != 0)
            {
                if ((power & 1) == 1)
                {
                    rv *= v;
                }

                v *= v;
                power >>= 1;
            }

            return rv;
        }

        public static IEnumerable<IEnumerable<T>> DifferentCombinations<T>(this IEnumerable<T> elements, int k)
        {
            return k == 0
                ? new[] { Array.Empty<T>() }
                : elements.SelectMany((e, i) =>
                    elements.Skip(i + 1).DifferentCombinations(k - 1).Select(c =>
                        new[] { e }.Concat(c)));
        }

        public static IObservable<int> ObservableIntervalLoopingList(this List<int> elements, double interval)
        {
            return Observable.Interval(TimeSpan.FromSeconds(interval))
                .Select(second => elements[(int)(second % elements.Count)]);
        }
    }
}
