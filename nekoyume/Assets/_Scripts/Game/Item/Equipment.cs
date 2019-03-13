using System;
using Nekoyume.Model;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public abstract class Equipment : ItemUsable
    {
        public bool equipped = false;
        private int _level = 0;
        private int _enchantCount = 0;

        public Equipment(Data.Table.Item data)
            : base(data)
        {
        }

        public override bool Use()
        {
            if (!equipped)
            {
                return Equip();
            }
            else
            {
                return Unequip();
            }
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

        public bool Enchant()
        {
            _enchantCount++;
            return true;
        }

        public bool LevelUp()
        {
            _level++;
            return true;
        }

        public abstract void UpdatePlayer(Player player);
    }
}
