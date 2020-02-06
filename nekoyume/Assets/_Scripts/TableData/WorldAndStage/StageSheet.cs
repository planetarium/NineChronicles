using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;

namespace Nekoyume.TableData
{
    [Serializable]
    public class StageSheet : Sheet<int, StageSheet.Row>
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
            // FIXME AudioController.MusicCode.StageGreen과 중복
            private const string DefaultBGM = "bgm_stage_green";
            
            public override int Key => Id;
            public int Id { get; private set; }
            public int CostAP { get; private set; }
            public int TurnLimit { get; private set; }
            public List<StatModifier> EnemyOptionalStatModifiers { get; private set; }
            public string Background { get; private set; }
            public string BGM { get; private set; }
            public List<RewardData> Rewards { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.TryParse(fields[0], out var id) ? id : 0;
                CostAP = int.TryParse(fields[1], out var costAP) ? costAP : 0;
                TurnLimit = int.TryParse(fields[2], out var turnLimit) ? turnLimit : 0;
                EnemyOptionalStatModifiers = new List<StatModifier>();
                if (int.TryParse(fields[3], out var atkOption) &&
                    atkOption != 0)
                {
                    EnemyOptionalStatModifiers.Add(new StatModifier(StatType.ATK, StatModifier.OperationType.Percentage, atkOption));
                    EnemyOptionalStatModifiers.Add(new StatModifier(StatType.SPD, StatModifier.OperationType.Percentage, atkOption));
                }
                if (int.TryParse(fields[4], out var defOption) &&
                    defOption != 0)
                {
                    EnemyOptionalStatModifiers.Add(new StatModifier(StatType.HP, StatModifier.OperationType.Percentage, defOption));
                    EnemyOptionalStatModifiers.Add(new StatModifier(StatType.DEF, StatModifier.OperationType.Percentage, defOption));
                    EnemyOptionalStatModifiers.Add(new StatModifier(StatType.HIT, StatModifier.OperationType.Percentage, defOption));
                }
                
                Background = fields[5];
                BGM = string.IsNullOrEmpty(fields[6])
                    ? DefaultBGM
                    : fields[6];
                Rewards = new List<RewardData>();
                for (var i = 0; i < 10; i++)
                {
                    var offset = i * 4;
                    if (!int.TryParse(fields[7 + offset], out var itemId))
                        continue;
                    
                    Rewards.Add(new RewardData(
                        itemId,
                        decimal.TryParse(fields[8 + offset], out var ratio) ? ratio : 0m,
                        int.TryParse(fields[9 + offset], out var min) ? min : 0,
                        int.TryParse(fields[10 + offset], out var max) ? max : 0
                    ));
                }
            }
        }
        
        public StageSheet() : base(nameof(StageSheet))
        {
        }
    }
}
