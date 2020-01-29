using System;
using Nekoyume.Game.Item;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    public class RangedWeapon : Weapon
    {
        public RangedWeapon(EquipmentItemSheet.Row data, Guid id) : base(data, id)
        {
        }
    }
}
