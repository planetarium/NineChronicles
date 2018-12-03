namespace Nekoyume.Game.Item
{
    public class Equipment : ItemUsable
    {
        private bool _isEquipped = false;
        private int _level = 0;
        private int _enchantCount = 0;

        public Equipment(Data.Table.Item data)
            : base(data)
        {
        }

        public override bool Use()
        {
            if (!_isEquipped)
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
            _isEquipped = true;
            return true;
        }

        public bool Unequip()
        {
            _isEquipped = false;
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
