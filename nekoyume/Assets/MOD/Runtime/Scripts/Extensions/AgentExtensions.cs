using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;

namespace NineChronicles.MOD
{
    public static class AgentExtensions
    {
        public static async UniTask<ItemSlotState> GetItemSlotStateAsync(
            this IAgent agent,
            Address avatarAddress)
        {
            var arenaItemSlotAddress = ItemSlotState.DeriveAddress(avatarAddress, BattleType.Arena);
            var arenaItemSlotValue = await agent.GetStateAsync(arenaItemSlotAddress);
            return arenaItemSlotValue is List l
                ? new ItemSlotState(l)
                : new ItemSlotState(BattleType.Arena);
        }

        public static async UniTask<List<RuneState>> GetRuneStatesAsync(
            this IAgent agent,
            Address avatarAddress,
            IEnumerable<RuneSlotInfo> runeSlotInfos)
        {
            IEnumerable<Address> runeStateAddresses;
            if (runeSlotInfos is null)
            {
                var arenaRuneSlotAddress = RuneSlotState.DeriveAddress(avatarAddress, BattleType.Arena);
                var arenaRuneSlotValue = await agent.GetStateAsync(arenaRuneSlotAddress);
                var arenaRuneSlotState = arenaRuneSlotValue is List l
                    ? new RuneSlotState(l)
                    : new RuneSlotState(BattleType.Arena);

                runeStateAddresses = arenaRuneSlotState.GetEquippedRuneSlotInfos().Select(slotInfo =>
                    RuneState.DeriveAddress(avatarAddress, slotInfo.RuneId));
            }
            else
            {
                runeStateAddresses = runeSlotInfos.Select(info =>
                                RuneState.DeriveAddress(avatarAddress, info.RuneId));
            }

            var runeStateValues = await agent.GetStateBulkAsync(runeStateAddresses);
            var runeStates = new List<RuneState>();
            foreach (var runeStateAddress in runeStateAddresses)
            {
                if (!runeStateValues.TryGetValue(runeStateAddress, out var runeStateValue))
                {
                    continue;
                }

                if (runeStateValue is List l)
                {
                    runeStates.Add(new RuneState(l));
                }
            }

            return runeStates;
        }
    }
}
