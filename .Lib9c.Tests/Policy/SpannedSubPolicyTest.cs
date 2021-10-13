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
                new SpannedSubPolicy<int>(-1, null, null, 1));
            // Bad end index
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SpannedSubPolicy<int>(10, 9, null, 1));
            // Same start index and end index should be fine
            spannedSubPolicy = new SpannedSubPolicy<int>(10, 10, null, 1);

            // Count indices in range
            Assert.Equal(11, indices.Where(i =>
                new SpannedSubPolicy<int>(10, 20, index => index % 30 == 0, 1).IsTargetRange(i)).Count());
            // No index in range satisfies predicate
            Assert.False(indices.Where(i =>
                new SpannedSubPolicy<int>(10, 20, index => index % 30 == 0, 1).IsTargetIndex(i)).Any());
            // Predicate of null counts everything in range
            Assert.Equal(11, indices.Where(i =>
                new SpannedSubPolicy<int>(10, 20, null, 1).IsTargetIndex(i)).Count());
            // Count target indices
            Assert.Equal(3, indices.Where(i =>
                new SpannedSubPolicy<int>(10, 20, index => index % 5 == 0, 1).IsTargetIndex(i)).Count());
            // Count indices in range for indefinite case
            Assert.Equal(90, indices.Where(i =>
                new SpannedSubPolicy<int>(10, null, index => index % 5 == 0, 1).IsTargetRange(i)).Count());
            // Count target indices for indefinite case
            Assert.Equal(18, indices.Where(i =>
                new SpannedSubPolicy<int>(10, null, index => index % 5 == 0, 1).IsTargetIndex(i)).Count());
        }
    }
}
