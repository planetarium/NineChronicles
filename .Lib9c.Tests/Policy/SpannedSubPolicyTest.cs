namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Nekoyume.BlockChain.Policy;
    using Xunit;

    public class SpannedSubPolicyTest
    {
        public SpannedSubPolicyTest()
        {
        }

        [Fact]
        public void Constructor()
        {
            SpannedSubPolicy<int> spannedSubPolicy;
            List<long> indices = Enumerable.Range(0, 100).Select(i => (long)i).ToList();

            // Bad start index
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SpannedSubPolicy<int>(-1, null, 1, 1));
            // Bad end index
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SpannedSubPolicy<int>(10, 9, 1, 1));
            // Same start index and end index should be fine
            spannedSubPolicy = new SpannedSubPolicy<int>(10, 10, 1, 1);
            // Bad interval
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SpannedSubPolicy<int>(10, 20, 0, 1));

            // Count indices in range
            Assert.Equal(11, indices.Where(i =>
                new SpannedSubPolicy<int>(10, 20, 30, 1).IsTargetRange(i)).Count());
            // Interval is too wide to have any target index.
            Assert.False(indices.Where(i =>
                new SpannedSubPolicy<int>(10, 20, 30, 1).IsTargetIndex(i)).Any());
            // Interval of 1 should not throw an error and count as expected.
            Assert.Equal(11, indices.Where(i =>
                new SpannedSubPolicy<int>(10, 20, 1, 1).IsTargetIndex(i)).Count());
            // Count target indices
            Assert.Equal(3, indices.Where(i =>
                new SpannedSubPolicy<int>(10, 20, 5, 1).IsTargetIndex(i)).Count());
            // Indefinite case
            Assert.Equal(90, indices.Where(i =>
                new SpannedSubPolicy<int>(10, null, 5, 1).IsTargetRange(i)).Count());
            // Indefinite case
            Assert.Equal(18, indices.Where(i =>
                new SpannedSubPolicy<int>(10, null, 5, 1).IsTargetIndex(i)).Count());
        }
    }
}
