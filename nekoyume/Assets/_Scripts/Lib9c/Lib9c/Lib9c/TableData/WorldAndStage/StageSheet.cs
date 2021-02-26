using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using static Nekoyume.TableData.TableExtensions;

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

            public int DropItemMin { get; private set; }
            public int DropItemMax { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = TryParseInt(fields[0], out var id) ? id : 0;
                CostAP = TryParseInt(fields[1], out var costAP) ? costAP : 0;
                TurnLimit = TryParseInt(fields[2], out var turnLimit) ? turnLimit : 0;
                EnemyOptionalStatModifiers = new List<StatModifier>();
                for (var i = 0; i < 6; i++)
                {
                    if (!TryParseInt(fields[3 + i], out var option) ||
                        option == 0)
                        continue;

                    switch (i)
                    {
                        case 0:
                            EnemyOptionalStatModifiers.Add(new StatModifier(StatType.HP, StatModifier.OperationType.Percentage, option));
                            break;
                        case 1:
                            EnemyOptionalStatModifiers.Add(new StatModifier(StatType.ATK, StatModifier.OperationType.Percentage, option));
                            break;
                        case 2:
                            EnemyOptionalStatModifiers.Add(new StatModifier(StatType.DEF, StatModifier.OperationType.Percentage, option));
                            break;
                        case 3:
                            EnemyOptionalStatModifiers.Add(new StatModifier(StatType.CRI, StatModifier.OperationType.Percentage, option));
                            break;
                        case 4:
                            EnemyOptionalStatModifiers.Add(new StatModifier(StatType.HIT, StatModifier.OperationType.Percentage, option));
                            break;
                        case 5:
                            EnemyOptionalStatModifiers.Add(new StatModifier(StatType.SPD, StatModifier.OperationType.Percentage, option));
                            break;
                    }
                    
                }
                
                Background = fields[9];
                BGM = string.IsNullOrEmpty(fields[10])
                    ? DefaultBGM
                    : fields[10];
                Rewards = new List<RewardData>();
                for (var i = 0; i < 10; i++)
                {
                    var offset = i * 4;
                    if (!TryParseInt(fields[11 + offset], out var itemId))
                        continue;
                    
                    Rewards.Add(new RewardData(
                        itemId,
                        TryParseDecimal(fields[12 + offset], out var ratio) ? ratio : 0m,
                        TryParseInt(fields[13 + offset], out var min) ? min : 0,
                        TryParseInt(fields[14 + offset], out var max) ? max : 0
                    ));
                }

                DropItemMin = TryParseInt(fields[51], out var dropMin) ? dropMin : 0;
                DropItemMax = TryParseInt(fields[52], out var dropMax) ? dropMax : 0;
            }
        }
        
        public StageSheet() : base(nameof(StageSheet))
        {
        }
    }
}
