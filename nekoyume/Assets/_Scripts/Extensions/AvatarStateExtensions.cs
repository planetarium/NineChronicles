using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using UnityEngine;
using static Lib9c.SerializeKeys;

namespace Nekoyume
{
    public static class AvatarStateExtensions
    {
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

        public static async Task<(List<ItemSlotState>, List<RuneSlotState>)> GetSlotStatesAsync(
            this AvatarState avatarState)
        {
            var avatarAddress = avatarState.address;

            var itemAddresses = new List<Address>
            {
                ItemSlotState.DeriveAddress(avatarAddress, BattleType.Adventure),
                ItemSlotState.DeriveAddress(avatarAddress, BattleType.Arena),
                ItemSlotState.DeriveAddress(avatarAddress, BattleType.Raid)
            };
            var itemBulk = await Game.Game.instance.Agent.GetStateBulk(itemAddresses);
            var itemStates = new List<ItemSlotState>();
            foreach (var value in itemBulk.Values)
            {
                if (value is List list)
                {
                    itemStates.Add(new ItemSlotState(list));
                }
            }

            var runeAddresses = new List<Address>
            {
                RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure),
                RuneSlotState.DeriveAddress(avatarAddress, BattleType.Arena),
                RuneSlotState.DeriveAddress(avatarAddress, BattleType.Raid)
            };
            var runeBulk = await Game.Game.instance.Agent.GetStateBulk(runeAddresses);
            var runeStates = new List<RuneSlotState>();
            foreach (var value in runeBulk.Values)
            {
                if (value is List list)
                {
                    runeStates.Add(new RuneSlotState(list));
                }
            }

            return (itemStates, runeStates);
        }
    }
}
