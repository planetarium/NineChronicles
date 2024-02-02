using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Bencodex.Types;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.Model;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume
{
    public static class AvatarStateExtensions
    {
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
            var itemBulk = await Game.Game.instance.Agent.GetStateBulkAsync(
                ReservedAddresses.LegacyAccount,
                itemAddresses);
            var itemSlotStates = new List<ItemSlotState>();
            foreach (var value in itemBulk.Values)
            {
                if (value is List list)
                {
                    itemSlotStates.Add(new ItemSlotState(list));
                }
            }

            var runeAddresses = new List<Address>
            {
                RuneSlotState.DeriveAddress(avatarAddress, BattleType.Adventure),
                RuneSlotState.DeriveAddress(avatarAddress, BattleType.Arena),
                RuneSlotState.DeriveAddress(avatarAddress, BattleType.Raid)
            };
            var runeBulk = await Game.Game.instance.Agent.GetStateBulkAsync(
                ReservedAddresses.LegacyAccount,
                runeAddresses);
            var runeSlotStates = new List<RuneSlotState>();
            foreach (var value in runeBulk.Values)
            {
                if (value is List list)
                {
                    runeSlotStates.Add(new RuneSlotState(list));
                }
            }

            return (itemSlotStates, runeSlotStates);
        }

        public static async Task<List<RuneState>> GetRuneStatesAsync(this AvatarState avatarState)
        {
            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            var runeIds = runeListSheet.Values.Select(x => x.Id).ToList();
            var addresses = runeIds.Select(id => RuneState.DeriveAddress(avatarState.address, id)).ToList();
            var bulk = await Game.Game.instance.Agent.GetStateBulkAsync(
                ReservedAddresses.LegacyAccount,
                addresses);
            var runeStates = new List<RuneState>();
            foreach (var value in bulk.Values)
            {
                if (value is List list)
                {
                    runeStates.Add(new RuneState(list));
                }
            }

            return runeStates;
        }
    }
}
