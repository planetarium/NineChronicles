using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume
{
    public static class AvatarExtensions
    {
        public static bool TryGetEquippedFullCostume(this AvatarState avatarState, out Costume fullCostume)
        {
            fullCostume = null;

            if (avatarState?.inventory is null)
            {
                return false;
            }

            var inventory = avatarState.inventory;
            fullCostume = inventory.Costumes.FirstOrDefault(e =>
                e.ItemSubType == ItemSubType.FullCostume &&
                e.Equipped);
            return fullCostume is not null;
        }

        public static int GetArmorIdForPortrait(this AvatarState avatarState)
        {
            return TryGetEquippedFullCostume(avatarState, out var fullCostume)
                ? fullCostume.Id
                : avatarState.GetArmorId();
        }

        // Copy from https://github.com/planetarium/lib9c/blob/1.28.0/Lib9c/Model/State/AvatarState.cs#L1121
        public static void EquipEquipments(this AvatarState avatarState, List<Guid> equipmentIds)
        {
            // 장비 해제.
            var inventoryEquipments = avatarState.inventory.Items
                .Select(i => i.item)
                .OfType<Equipment>()
                .Where(i => i.equipped)
                .ToImmutableHashSet();
#pragma warning disable LAA1002
            foreach (var equipment in inventoryEquipments)
#pragma warning restore LAA1002
            {
                equipment.Unequip();
            }

            // 장비 장착.
            foreach (var equipmentId in equipmentIds)
            {
                if (!avatarState.inventory.TryGetNonFungibleItem(equipmentId, out ItemUsable outNonFungibleItem))
                {
                    continue;
                }

                ((Equipment) outNonFungibleItem).Equip();
            }
        }
    }
}
