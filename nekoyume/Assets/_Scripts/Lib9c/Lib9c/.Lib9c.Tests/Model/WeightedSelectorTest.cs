namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.Battle;
    using Xunit;

    public class WeightedSelectorTest
    {
        [Fact]
        public void SelectV2Single()
        {
            var i = 0;
            var result = new Dictionary<int, int>();
            var result2 = new Dictionary<int, int>();
            while (i < 10000)
            {
                var selector = GetSelector();
                var selector2 = GetSelector();
                var ids = selector.Select(1);
                var ids2 = selector2.SelectV2(1);
                foreach (var id in ids)
                {
                    if (result.ContainsKey(id))
                    {
                        result[id] += 1;
                    }
                    else
                    {
                        result[id] = 1;
                    }
                }

                foreach (var id in ids2)
                {
                    if (result2.ContainsKey(id))
                    {
                        result2[id] += 1;
                    }
                    else
                    {
                        result2[id] = 1;
                    }
                }

                i++;
            }

            var ordered = result
                .OrderByDescending(r => r.Value)
                .Select(r => r.Key)
                .ToArray();

            var ordered2 = result2
                .OrderByDescending(r => r.Value)
                .Select(r => r.Key)
                .ToArray();

            Assert.Equal(new[] { 1, 2, 3, 4 }, ordered);
            Assert.Equal(new[] { 1, 2, 3, 4 }, ordered2);
        }

        [Fact]
        public void SelectV2Multiple()
        {
            var i = 0;
            var result = new Dictionary<int, int>();
            var result2 = new Dictionary<int, int>();
            while (i < 100000)
            {
                var selector = GetSelector();
                var selector2 = GetSelector();
                var ids = selector.Select(2);
                var ids2 = selector2.SelectV2(2);
                foreach (var id in ids)
                {
                    if (result.ContainsKey(id))
                    {
                        result[id] += 1;
                    }
                    else
                    {
                        result[id] = 1;
                    }
                }

                foreach (var id in ids2)
                {
                    if (result2.ContainsKey(id))
                    {
                        result2[id] += 1;
                    }
                    else
                    {
                        result2[id] = 1;
                    }
                }

                i++;
            }

            var ordered = result
                .OrderByDescending(r => r.Value)
                .Select(r => r.Key)
                .ToArray();

            var ordered2 = result2
                .OrderByDescending(r => r.Value)
                .Select(r => r.Key)
                .ToArray();

            Assert.Equal(new[] { 1, 4, 2, 3 }, ordered);
            Assert.Equal(new[] { 1, 2, 3, 4 }, ordered2);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void SelectV3(int count)
        {
            var i = 0;
            var result = new Dictionary<int, int>();
            while (i < 10000)
            {
                var selector3 = GetSelector();
                var ids = selector3.SelectV3(count);
                foreach (var id in ids)
                {
                    if (result.ContainsKey(id))
                    {
                        result[id] += 1;
                    }
                    else
                    {
                        result[id] = 1;
                    }
                }

                i++;
            }

            var ordered = result
                .OrderByDescending(r => r.Value)
                .Select(r => r.Key)
                .ToArray();

            Assert.Equal(new[] { 1, 2, 3, 4 }, ordered);
        }

        private static WeightedSelector<int> GetSelector()
        {
            var selector = new WeightedSelector<int>(new TestRandom());
            selector.Add(1, 0.48m);
            selector.Add(2, 0.38m);
            selector.Add(3, 0.09m);
            selector.Add(4, 0.05m);
            return selector;
        }
    }
}
