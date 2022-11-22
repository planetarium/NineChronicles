using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Model.State;
using UnityEngine;
using static Lib9c.SerializeKeys;

namespace Nekoyume
{
    public static class AvatarStateExtensions
    {
        public static ArenaAvatarState ToArenaAvatarState(this AvatarState avatarState)
        {
            var arenaAvatarState = new ArenaAvatarState(avatarState);
            arenaAvatarState.UpdateCostumes(avatarState.inventory.Costumes
                .Where(costume => costume.Equipped)
                .Select(e => e.NonFungibleId)
                .ToList());
            arenaAvatarState.UpdateEquipment(avatarState.inventory.Equipments
                .Where(equipment => equipment.Equipped)
                .Select(e => e.NonFungibleId)
                .ToList());
            return arenaAvatarState;
        }

        public static AvatarState ApplyToInventory(
            this AvatarState avatarState,
            ArenaAvatarState arenaAvatarState)
        {
            avatarState.inventory = avatarState.inventory.Apply(arenaAvatarState);
            return avatarState;
        }

        public static AvatarState CloneAndApplyToInventory(
            this AvatarState avatarState,
            ArenaAvatarState arenaAvatarState) =>
            new AvatarState(avatarState)
            {
                inventory = avatarState.inventory.CloneAndApply(arenaAvatarState),
            };

        public static AvatarState UpdateAvatarStateV2(
            this AvatarState avatarState,
            Address address,
            IAccountStateDelta states)
        {
            var addresses = new List<Address>
            {
                address,
            };
            string[] keys =
            {
                LegacyInventoryKey,
                LegacyWorldInformationKey,
                LegacyQuestListKey,
            };
            addresses.AddRange(keys.Select(key => address.Derive(key)));
            var serializedValues = states.GetStates(addresses);
            if (!(serializedValues[0] is Dictionary serializedAvatar))
            {
                Debug.LogWarning($"No avatar state ({address.ToHex()})");
                return null;
            }

            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var serializedValue = serializedValues[i + 1];
                if (serializedValue is null)
                {
                    Debug.Log($"\"{key}\" is empty in \"{address.ToHex()}\"");
                    continue;
                }

                serializedAvatar = serializedAvatar.SetItem(key, serializedValue);
            }

            var newAvatarState = new AvatarState(serializedAvatar);
            newAvatarState.questList ??= avatarState.questList;
            newAvatarState.inventory ??= avatarState.inventory;
            newAvatarState.worldInformation ??= avatarState.worldInformation;
            return newAvatarState;
        }
    }
}
