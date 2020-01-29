using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Equipment : ItemUsable
    {
        public bool equipped = false;
        public int level;
        public int levelStats = 5;

        public new EquipmentItemSheet.Row Data { get; }
        public StatType UniqueStatType => Data.Stat.Type;

        public Equipment(EquipmentItemSheet.Row data, Guid id) : base(data, id)
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

        public void LevelUp()
        {
            level++;
            StatsMap.AddStatValue(Data.Stat.Type, levelStats);
        }

        public override string GetLocalizedName()
        {
            var name = base.GetLocalizedName();

            return level > 0
                ? $"<color=#{GetColorHexByGrade()}>+{level}</color> {name}"
                : name;
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"equipped"] = new Bencodex.Types.Boolean(equipped),
                [(Text)"level"] = (Integer)level,
            }.Union((Dictionary)base.Serialize()));

        public List<object> GetOptions()
        {
            var options = new List<object>();
            options.AddRange(Skills);
            options.AddRange(BuffSkills);
            foreach (var statMapEx in StatsMap.GetAdditionalStats())
            {
                options.Add(new StatModifier(statMapEx.StatType, StatModifier.OperationType.Add, statMapEx.AdditionalValueAsInt));
            }

            return options;
        }
    }
}
