using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Equipment : ItemUsable
    {
        public bool equipped = false;
        public int level;

        public new EquipmentItemSheet.Row Data { get; }
        public StatType UniqueStatType => Data.Stat.Type;

        public decimal GetIncrementAmountOfEnhancement(int toLevel)
        {
            if (Data.ElementalType == ElementalType.Normal)
            {
                return StatsMap.GetStat(UniqueStatType, true) * 0.1m;
            }

            return toLevel == 4 || toLevel == 7 || toLevel == 10
                ? StatsMap.GetStat(UniqueStatType, true) * 0.3m
                : StatsMap.GetStat(UniqueStatType, true) * 0.1m;
        }


        public Equipment(EquipmentItemSheet.Row data, Guid id, long requiredBlockIndex)
            : base(data, id, requiredBlockIndex)
        {
            Data = data;
        }

        public bool Equip()
        {
            equipped = true;
            return true;
        }

        public bool Unequip()
        {
            equipped = false;
            return true;
        }

        // FIXME: 기본 스탯을 복리로 증가시키고 있는데, 단리로 증가시켜야 한다.
        // 이를 위해서는 기본 스탯을 유지하면서 추가 스탯에 더해야 하는데, UI 표현에 문제가 생기기 때문에 논의 후 개선한다.
        // 장비가 보유한 스킬의 확률과 수치 강화가 필요한 상태이다.
        public void LevelUp()
        {
            level++;
            StatsMap.AddStatValue(UniqueStatType, GetIncrementAmountOfEnhancement(level));
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "equipped"] = new Bencodex.Types.Boolean(equipped),
                [(Text) "level"] = (Integer) level,
            }.Union((Dictionary) base.Serialize()));

        public List<object> GetOptions()
        {
            var options = new List<object>();
            options.AddRange(Skills);
            options.AddRange(BuffSkills);
            foreach (var statMapEx in StatsMap.GetAdditionalStats())
            {
                options.Add(new StatModifier(statMapEx.StatType, StatModifier.OperationType.Add,
                    statMapEx.AdditionalValueAsInt));
            }

            return options;
        }
    }
}
