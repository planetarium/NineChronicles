using System;
using System.Collections.Generic;

namespace Nekoyume.TableData
{
    [Serializable]
    public class StageRewardSheet : Sheet<int, StageRewardSheet.Row>
    {
        [Serializable]
        public class RewardData
        {
            public int ItemId { get; }
            public decimal Ratio { get; }
            public int Min { get; }
            public int Max { get; }

            public RewardData(int itemId, decimal ratio, int min, int max)
            {
                ItemId = itemId;
                Ratio = ratio;
                Min = min;
                Max = max;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public List<RewardData> Rewards { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.TryParse(fields[0], out var id) ? id : 0;
                Rewards = new List<RewardData>();
                for (var i = 0; i < 10; i++)
                {
                    var offset = i * 4;
                    if (!int.TryParse(fields[1 + offset], out var itemId))
                        continue;
                    
                    Rewards.Add(new RewardData(
                        itemId,
                        decimal.TryParse(fields[2 + offset], out var ratio) ? ratio : 0m,
                        int.TryParse(fields[3 + offset], out var min) ? min : 0,
                        int.TryParse(fields[4 + offset], out var max) ? max : 0
                    ));
                }
            }
        }
        
        public StageRewardSheet() : base(nameof(StageRewardSheet))
        {
        }
    }
}
