using System;
using Nekoyume.TableData;

namespace Nekoyume.Game.Item
{
    [Serializable]
    public class Equipment : ItemUsable
    {
        public bool equipped = false;

        public new EquipmentItemSheet.Row Data { get; }

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
    }
}
