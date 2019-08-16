using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;

namespace Nekoyume.TableData
{
    [Serializable]
    public class StageRewardSheet : Sheet<int, StageRewardSheet.Row>
    {
        [Serializable]
        public struct RewardData
        {
            public int ItemId { get; }
            public float Ratio { get; }
            public int Min { get; }
            public int Max { get; }

            public RewardData(int itemId, float ratio, int min, int max)
            {
                ItemId = itemId;
                Ratio = ratio;
                Min = min;
                Max = max;
            }
        }
        
        [Serializable]
        public struct Row : ISheetRow<int>
        {
            public int Id { get; private set; }
            public List<RewardData> Rewards { get; private set; }

            public int Key => Id;
            
            public void Set(string[] fields)
            {
                Id = int.TryParse(fields[0], out var id) ? id : 0;
                Rewards = new List<RewardData>();
                for (var i = 0; i < 5; i++)
                {
                    var offset = i * 4;
                    Rewards.Add(new RewardData(
                            int.TryParse(fields[1 + offset], out var itemId) ? itemId : 0,
                            float.TryParse(fields[2 + offset], out var ratio) ? ratio : 0f,
                            int.TryParse(fields[3 + offset], out var min) ? min : 0,
                            int.TryParse(fields[4 + offset], out var max) ? max : 0
                        ));
                }
            }
        }
    }
}
