namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Nekoyume.Model.Quest;
    using Xunit;

    public class QuestRewardTest
    {
        [Fact]
        public void Serialize()
        {
            var reward = new QuestReward(new Dictionary<int, int>()
            {
                [2] = 1,
                [1] = 1,
            });

            var expected = new[]
            {
                new Tuple<int, int>(1, 1),
                new Tuple<int, int>(2, 1),
            };

            Assert.Equal(expected.OrderBy(t => t.Item1), reward.ItemMap.OrderBy(t => t.Item1));

            var serialized = reward.Serialize();
            var des = new QuestReward((Dictionary)serialized);

            Assert.Equal(reward.ItemMap.OrderBy(t => t.Item1), des.ItemMap.OrderBy(t => t.Item1));

            Assert.Equal(serialized, des.Serialize());
        }
    }
}
