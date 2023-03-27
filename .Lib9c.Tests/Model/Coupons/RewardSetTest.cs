#nullable enable
namespace Lib9c.Tests.Action.Coupons
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Nekoyume.Model.Coupons;
    using Xunit;

    public class RewardSetTest
    {
        [Fact]
        public void Constructors()
        {
            var emptySet = default(RewardSet);
            Assert.Equal(emptySet, new RewardSet(ImmutableDictionary<int, uint>.Empty));
            Assert.Equal(emptySet, new RewardSet(Enumerable.Empty<(int, uint)>()));
            Assert.Equal(emptySet, new RewardSet(new (int, uint)[0]));

            var set = new RewardSet(ImmutableDictionary<int, uint>.Empty.Add(1, 2).Add(3, 4));
            Assert.Equal(set, new RewardSet(new List<(int, uint)> { (1, 2U), (3, 4U) }));
            Assert.Equal(set, new RewardSet((1, 2U), (3, 4U)));
        }

        [Fact]
        public void Equality()
        {
            var emptySet = default(RewardSet);
            var set = new RewardSet((1, 2U), (3, 4U));
            var set2 = new RewardSet((1, 2U), (3, 5U));
            var set3 = new RewardSet((1, 2U), (2, 4U));
            var set4 = new RewardSet((1, 2U));
            Assert.False(set.Equals(emptySet));
            Assert.True(set.Equals(set));
            Assert.False(set.Equals(set2));
            Assert.False(set.Equals(set3));
            Assert.False(set.Equals(set4));
            Assert.False(set.Equals((object)emptySet));
            Assert.True(set.Equals((object)set));
            Assert.False(set.Equals((object)set2));
            Assert.False(set.Equals((object)set3));
            Assert.False(set.Equals((object)set4));
            Assert.False(set.Equals((object?)null));
            Assert.NotEqual(set.GetHashCode(), emptySet.GetHashCode());
            Assert.Equal(set.GetHashCode(), set.GetHashCode());
            Assert.NotEqual(set.GetHashCode(), set2.GetHashCode());
            Assert.NotEqual(set.GetHashCode(), set3.GetHashCode());
            Assert.NotEqual(set.GetHashCode(), set4.GetHashCode());
        }

        [Fact]
        public void Serialize()
        {
            var emptySet = default(RewardSet);
            var emptySerialized = emptySet.Serialize();
            Assert.Equal(Bencodex.Types.Dictionary.Empty, emptySerialized);

            var set = new RewardSet((1, 2U), (3, 4U));
            var serialized = set.Serialize();
            Assert.Equal(
                Bencodex.Types.Dictionary.Empty.Add("1", 2).Add("3", 4),
                serialized
            );
        }

        [Fact]
        public void Deserialize()
        {
            var deserializedEmptySet = new RewardSet(Bencodex.Types.Dictionary.Empty);
            var expectedEmptySet = default(RewardSet);
            Assert.Equal(expectedEmptySet, deserializedEmptySet);

            var deserializedSet =
                new RewardSet(Bencodex.Types.Dictionary.Empty.Add("1", 2).Add("3", 4));
            var expectedSet = new RewardSet((1, 2U), (3, 4U));
            Assert.Equal(expectedSet, deserializedSet);
        }
    }
}
