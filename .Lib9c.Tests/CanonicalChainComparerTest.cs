namespace Lib9c.Tests
{
    using System;
    using System.Linq;
    using System.Numerics;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Blocks;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class CanonicalChainComparerTest
    {
        private readonly BlockPerception[] _blocks;
        private readonly DateTimeOffset _perceivedTime;

        public CanonicalChainComparerTest()
        {
            DateTimeOffset p = DateTimeOffset.UtcNow;
            _perceivedTime = p;
            _blocks = new[]
            {
                new BlockPerception(new MockBlock { Index = 10001, TotalDifficulty = 50010 }, p),
                new BlockPerception(new MockBlock { Index = 10002, TotalDifficulty = 50009 }, p),
                new BlockPerception(new MockBlock { Index = 9999,  TotalDifficulty = 50011 }, p),
            };
        }

        [Fact]
        public void WhenAuthorizedMinersAlive()
        {
            IOrderedEnumerable<BlockPerception> ordered = _blocks.OrderBy(
                p => p,
                new CanonicalChainComparer(
                    new AuthorizedMinersState(new Address[0], 50, 20000),
                    TimeSpan.FromHours(3)
                )
            );

            Assert.Equal(_blocks.Reverse(), ordered);
        }

        [Fact]
        public void WhenAuthorizedMinersNoLongerAlive()
        {
            BlockPerception[] oredered = _blocks.OrderBy(
                p => p,
                new CanonicalChainComparer(
                    new AuthorizedMinersState(new Address[0], 50, 5000),
                    TimeSpan.FromHours(3)
                )
            ).ToArray();

            Assert.Equal(new[] { _blocks[1], _blocks[0], _blocks[2] }, oredered);
        }

        public class MockBlock : IBlockExcerpt
        {
            public int ProtocolVersion =>
                Block<PolymorphicAction<ActionBase>>.CurrentProtocolVersion;

            public long Index { get; set; }

            public BlockHash Hash => new BlockHash(new byte[32]);

            public BigInteger TotalDifficulty { get; set; }
        }
    }
}
