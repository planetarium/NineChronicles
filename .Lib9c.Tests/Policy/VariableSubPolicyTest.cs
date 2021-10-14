namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Nekoyume.BlockChain.Policy;
    using Xunit;

    public class VariableSubPolicyTest
    {
        public VariableSubPolicyTest()
        {
        }

        [Fact]
        public void Constructor()
        {
            VariableSubPolicy<bool> variableSubPolicy;
            List<long> indices = Enumerable.Range(0, 100).Select(i => (long)i).ToList();

            SpannedSubPolicy<bool> first = new SpannedSubPolicy<bool>(10, null, null, true);
            SpannedSubPolicy<bool> badSecond = new SpannedSubPolicy<bool>(5, null, index => index % 2 == 0, true);
            SpannedSubPolicy<bool> second = new SpannedSubPolicy<bool>(20, 50, index => index % 2 == 0, true);
            SpannedSubPolicy<bool> third = new SpannedSubPolicy<bool>(30, 40, null, true);
            SpannedSubPolicy<bool> fourth = new SpannedSubPolicy<bool>(50, 80, index => index % 5 == 0, true);

            // Should be fine.
            variableSubPolicy = VariableSubPolicy<bool>
                .Create(false)
                // 10 ~ 19 => count 10
                .Add(first)
                // 20 ~ 29 && mod 2 => count 5
                .Add(second)
                // 30 ~ 40 => count 11
                .Add(third)
                // 50 ~ 80 && mod 5 => count 7
                .Add(fourth);

            Assert.Equal(4, variableSubPolicy.SpannedSubPolicies.Count);
            Assert.Equal(10, variableSubPolicy.SpannedSubPolicies[0].StartIndex);
            Assert.Equal(19, variableSubPolicy.SpannedSubPolicies[0].EndIndex);
            Assert.Equal(20, variableSubPolicy.SpannedSubPolicies[1].StartIndex);
            Assert.Equal(29, variableSubPolicy.SpannedSubPolicies[1].EndIndex);
            Assert.Equal(30, variableSubPolicy.SpannedSubPolicies[2].StartIndex);
            Assert.Equal(40, variableSubPolicy.SpannedSubPolicies[2].EndIndex);
            Assert.Equal(50, variableSubPolicy.SpannedSubPolicies[3].StartIndex);
            Assert.Equal(80, variableSubPolicy.SpannedSubPolicies[3].EndIndex);

            Assert.Equal(
                indices.Where(i => variableSubPolicy.Getter(i)).Count(),
                variableSubPolicy.SpannedSubPolicies
                    .Select(s => indices.Where(i => s.IsTargetIndex(i)).Count())
                    .Sum());
            Assert.Equal(33, indices.Where(i => variableSubPolicy.Getter(i)).Count());
            // Check first one is no longer indefinite and cut short.
            Assert.True(first.Indefinite);
            Assert.False(variableSubPolicy.SpannedSubPolicies.First().Indefinite);
            Assert.Equal(19, variableSubPolicy.SpannedSubPolicies.First().EndIndex);

            // Out of order addition should not work.
            Assert.Throws<ArgumentOutOfRangeException>(() => VariableSubPolicy<bool>
                .Create(false)
                .Add(fourth)
                .Add(third));
            Assert.Throws<ArgumentOutOfRangeException>(() => VariableSubPolicy<bool>
                .Create(false)
                .Add(first)
                .Add(badSecond));

            // Create using AddRange().
            variableSubPolicy = VariableSubPolicy<bool>
                .Create(false)
                .AddRange(new List<SpannedSubPolicy<bool>>() { first, second, third, fourth }.ToImmutableList());
            Assert.Throws<ArgumentOutOfRangeException>(() => VariableSubPolicy<bool>
                .Create(false)
                .AddRange(new List<SpannedSubPolicy<bool>>() { second, first }.ToImmutableList()));
        }
    }
}
