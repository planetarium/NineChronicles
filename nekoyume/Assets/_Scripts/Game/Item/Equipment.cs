using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
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

        public void LevelUp()
        {
            level++;
            foreach (var statData in Data.Stats)
            {
                StatsMap.SetStatAdditionalValue(statData.StatType, level * levelStats);
            }
        }
        
        public override string GetLocalizedName()
        {
            var name = base.GetLocalizedName();

            return level > 0
                ? $"{name} +{level}"
                : name;
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "equipped"] = new Bencodex.Types.Boolean(equipped),
                [(Text) "level"] = (Integer) level,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));

    }
}
