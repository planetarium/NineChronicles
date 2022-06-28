using Libplanet;
using Nekoyume;
using Nekoyume.State;
using NUnit.Framework;

namespace Tests.EditMode.State
{
    public class RxPropsTest
    {
        [DatapointSource]
        private (
            (Address avatarAddr, int score)[] source,
            (Address avatarAddr, int score, int rank)[] expect)[]
            _valuesFor_AddRank = new[]
            {
                (
                    new[]
                    {
                        (Addresses.Admin, 1000),
                        (Addresses.Arena, 1001),
                        (Addresses.Blacksmith, 1002),
                        (Addresses.Credits, 1001),
                        (Addresses.Ranking, 1001),
                        (Addresses.Shop, 1001),
                        (Addresses.Admin, 1000),
                    },
                    new[]
                    {
                        (Addresses.Blacksmith, 1002, 1),
                        (Addresses.Shop, 1001, 5),
                        (Addresses.Ranking, 1001, 5),
                        (Addresses.Credits, 1001, 5),
                        (Addresses.Arena, 1001, 5),
                        (Addresses.Admin, 1000, 7),
                        (Addresses.Admin, 1000, 7),
                    }
                )
            };

        [Theory]
        public void AddRank((
            (Address avatarAddr, int score)[] source,
            (Address avatarAddr, int score, int rank)[] expect) tuple)
        {
            var (source, expect) = tuple;
            var result = RxProps.AddRank(source);
            Assert.AreEqual(expect.Length, result.Length);
            for (var i = 0; i < expect.Length; i++)
            {
                var e = expect[i];
                var r = result[i];
                Assert.AreEqual(e.avatarAddr, r.avatarAddr);
                Assert.AreEqual(e.score, r.score);
                Assert.AreEqual(e.rank, r.rank);
            }
        }
    }
}
