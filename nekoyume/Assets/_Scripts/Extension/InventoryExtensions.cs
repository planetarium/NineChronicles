using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume
{
    public static class InventoryExtensions
    {
        public static Inventory Apply(
            this Inventory inventory,
            ArenaAvatarState arenaAvatarState)
        {
            var nonFungibleIdsToEquip = new List<Guid>(arenaAvatarState.Costumes);
            nonFungibleIdsToEquip.AddRange(arenaAvatarState.Equipments);
            foreach (var itemBase in inventory.Items.Select(e => e.item))
            {
                if (!(itemBase is IEquippableItem equippableItem))
                {
                    continue;
                }

                if (!(itemBase is INonFungibleItem nonFungibleItem) ||
                    !nonFungibleIdsToEquip.Contains(nonFungibleItem.NonFungibleId))
                {
                    equippableItem.Unequip();
                    continue;
                }

                equippableItem.Equip();
                nonFungibleIdsToEquip.Remove(nonFungibleItem.NonFungibleId);
            }

            return inventory;
        }

        public static Inventory CloneAndApply(
            this Inventory inventory,
            ArenaAvatarState arenaAvatarState)
        {
            var value = inventory.Serialize();
            var result = new Inventory((List)value);
            result.Apply(arenaAvatarState);
            return result;
        }
    }
}
