using System.Collections.Generic;
using System.Linq;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.TableData;

namespace Nekoyume.Battle
{
    public static class RewardSelector
    {
        public static List<ItemBase> Select(
            IRandom random,
            WeeklyArenaRewardSheet weeklyArenaRewardSheet,
            MaterialItemSheet materialItemSheet,
            int playerLevel,
            int maxCount = 1
        )
        {
            var rewards = new List<ItemBase>();
            var itemSelector = new WeightedSelector<StageSheet.RewardData>(random);
            foreach (var row in weeklyArenaRewardSheet.OrderedList)
            {
                var reward = row.Reward;
                if (reward.RequiredLevel <= playerLevel)
                {
                    itemSelector.Add(reward, reward.Ratio);
                }
            }

            while (rewards.Count < maxCount)
            {
                try
                {
                    var data = itemSelector.Select(1).First();
                    if (materialItemSheet.TryGetValue(data.ItemId, out var itemData))
                    {
                        var count = random.Next(data.Min, data.Max + 1);
                        for (var i = 0; i < count; i++)
                        {
                            var item = ItemFactory.CreateMaterial(itemData);
                            if (rewards.Count < maxCount)
                            {
                                rewards.Add(item);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                catch (ListEmptyException)
                {
                    break;
                }
            }

            rewards = rewards.OrderBy(r => r.Id).ToList();
            return rewards;
        }
    }
}
