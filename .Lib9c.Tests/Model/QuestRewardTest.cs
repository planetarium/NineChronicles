namespace Lib9c.Tests.Model
{
    using System.Collections.Generic;
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

            Assert.Equal(
                new Dictionary<int, int>()
                {
                    [1] = 1,
                    [2] = 1,
                },
                reward.ItemMap
            );

            var serialized = reward.Serialize();
            var des = new QuestReward((Dictionary)serialized);

            Assert.Equal(reward.ItemMap, des.ItemMap);

            Assert.Equal(serialized, des.Serialize());
        }
    }
}
