using Nekoyume.Game;

// NOTE: This file is copied from Assets/_Scripts/Lib9c/lib9c/.Lib9c.Tests/Util/InitializeUtil.cs
namespace BalanceTool.Util
{
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;

    public static class InitializeUtil
    {
        public static (
            TableSheets tableSheets,
            Address agentAddr,
            Address avatarAddr,
            IAccount initialStatesWithAvatarStateV1,
            IAccount initialStatesWithAvatarStateV2
            ) InitializeStates(
                Address? adminAddr = null,
                Address? agentAddr = null,
                int avatarIndex = 0,
                bool unlockAllWorlds = true,
                bool clearAllWorlds = true,
                bool isDevEx = false,
                Dictionary<string, string> sheetsOverride = null)
        {
            adminAddr ??= new PrivateKey().Address;
            var context = new ActionContext();
            var states = new Account(MockState.Empty).SetState(
                Addresses.Admin,
                new AdminState(adminAddr.Value, long.MaxValue).Serialize());

            var goldCurrency = Currency.Legacy(
                "NCG",
                2,
                minters: default
            );
            var goldCurrencyState = new GoldCurrencyState(goldCurrency);
            states = states
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .MintAsset(context, goldCurrencyState.address, goldCurrency * 1_000_000_000);

            var tuple = InitializeTableSheets(states, isDevEx, sheetsOverride);
            states = tuple.states;
            var tableSheets = new TableSheets(tuple.sheets);
            var gameConfigState = new GameConfigState(tuple.sheets[nameof(GameConfigSheet)]);
            states = states.SetState(gameConfigState.address, gameConfigState.Serialize());

            agentAddr ??= new PrivateKey().Address;
            var avatarAddr = Addresses.GetAvatarAddress(agentAddr.Value, avatarIndex);
            var agentState = new AgentState(agentAddr.Value);
            var avatarState = new AvatarState(
                avatarAddr,
                agentAddr.Value,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                avatarAddr.Derive("ranking_map"));
            if (unlockAllWorlds)
            {
                var unlockedWorldIdsAddress = avatarAddr.Derive("world_ids");
                var worldIds = tableSheets.WorldSheet.OrderedList.Select(r => r.Id);
                states = states.SetState(
                    unlockedWorldIdsAddress,
                    new List(worldIds.Select(i => i.Serialize())).Serialize());
            }

            if (clearAllWorlds)
            {
                avatarState.worldInformation = new WorldInformation(
                    blockIndex: 0,
                    worldSheet: tableSheets.WorldSheet,
                    openAllOfWorldsAndStages: true);
            };

            agentState.avatarAddresses.Add(avatarIndex, avatarAddr);

            var initialStatesWithAvatarStateV1 = states
                .SetState(agentAddr.Value, agentState.Serialize())
                .SetState(avatarAddr, avatarState.Serialize());
            var initialStatesWithAvatarStateV2 = states
                .SetState(agentAddr.Value, agentState.Serialize())
                .SetState(avatarAddr, avatarState.SerializeV2())
                .SetState(
                    avatarAddr.Derive(SerializeKeys.LegacyInventoryKey),
                    avatarState.inventory.Serialize())
                .SetState(
                    avatarAddr.Derive(SerializeKeys.LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize())
                .SetState(
                    avatarAddr.Derive(SerializeKeys.LegacyQuestListKey),
                    avatarState.questList.Serialize());

            return (
                tableSheets,
                agentAddr.Value,
                avatarAddr,
                initialStatesWithAvatarStateV1,
                initialStatesWithAvatarStateV2);
        }

        public static (IAccount states, Dictionary<string, string> sheets)
            InitializeTableSheets(
                IAccount states,
                bool isDevEx = false,
                Dictionary<string, string> sheetsOverride = null)
        {
            var sheets = TableSheetsHelper.ImportSheets();
            if (sheetsOverride != null)
            {
                foreach (var (key, value) in sheetsOverride)
                {
                    sheets[key] = value;
                }
            }

            foreach (var (key, value) in sheets)
            {
                states = states.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            return (states, sheets);
        }
    }
}
