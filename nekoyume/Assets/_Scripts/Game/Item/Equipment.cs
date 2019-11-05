using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.TableData;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Equipment : ItemUsable
    {
        public bool equipped = false;

        public new EquipmentItemSheet.Row Data { get; }

        public int level;
        public int levelStats = 5;

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
        
        public bool TryGetBaseStat(out StatType statType, out int value, bool ignoreAdditional = false)
        {
            statType = Data.Stat.Type;
            value = StatsMap.GetValue(statType, ignoreAdditional);
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
                ? $"<color=#{GetColorHexByGrade()}>+{level} {name}</color>"
                : $"<color=#{GetColorHexByGrade()}>{name}</color>";
        }

        private string GetColorHexByGrade()
        {
            switch (Data.Grade)
            {
                case 1:
                    return GameConfig.ColorHexForGrade1;
                case 2:
                    return GameConfig.ColorHexForGrade2;
                case 3:
                    return GameConfig.ColorHexForGrade3;
                case 4:
                    return GameConfig.ColorHexForGrade4;
                case 5:
                    return GameConfig.ColorHexForGrade5;
                default:
                    return GameConfig.ColorHexForGrade1;
            }
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "equipped"] = new Bencodex.Types.Boolean(equipped),
                [(Text) "level"] = (Integer) level,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

        public List<object> GetOptions()
        {
            var options = new List<object>();
            options.AddRange(Skills);
            options.AddRange(BuffSkills);
            if (StatsMap.HasAdditionalStats)
            {
                options.Add(StatsMap);
            }
            return options;
        }
    }
}
