namespace Nekoyume.Game.Item
{
    public class Equipment : ItemUsable
    {
        public bool IsEquipped = false;
        private int _level = 0;
        private int _enchantCount = 0;

        public Equipment(Data.Table.Item data)
            : base(data)
        {
        }

        public override bool Use()
        {
            if (!IsEquipped)
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
            IsEquipped = true;
            return true;
        }

        public bool Unequip()
        {
            IsEquipped = false;
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
    }
}
