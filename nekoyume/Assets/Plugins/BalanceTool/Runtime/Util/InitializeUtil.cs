// NOTE: This file is copied from Assets/_Scripts/Lib9c/lib9c/.Lib9c.Tests/Util/InitializeUtil.cs
using Lib9c;
using Libplanet.Mocks;

namespace BalanceTool.Runtime.Util
{
    using System.Collections.Generic;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.Module;
    using Nekoyume.TableData;
    using BalanceTool.Util;
    using Nekoyume.Game;
    using Nekoyume.Helper;

    public static class InitializeUtil
    {
        public static (
            TableSheets tableSheets,
            Address agentAddr,
            Address avatarAddr,
            IWorld initialStatesWithAvatarStateV2
            ) InitializeStates(
                Address? adminAddr = null,
                Address? agentAddr = null,
                int avatarIndex = 0,
                bool isDevEx = false,
                Dictionary<string, string> sheetsOverride = null)
        {
            adminAddr ??= new PrivateKey().Address;
            var context = new ActionContext();
            var states = new World(MockWorldState.CreateModern()).SetLegacyState(
                Addresses.Admin,
                new AdminState(adminAddr.Value, long.MaxValue).Serialize());

            var goldCurrency = Currency.Legacy(
                "NCG",
                2,
                minters: default
            );
            var goldCurrencyState = new GoldCurrencyState(goldCurrency);
            states = states
                .SetLegacyState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .MintAsset(context, goldCurrencyState.address, goldCurrency * 1_000_000_000);

            var tuple = InitializeTableSheets(states, isDevEx, sheetsOverride);
            states = tuple.states;
            var tableSheets = TableSheets.MakeTableSheets(tuple.sheets);
            var gameConfigState = new GameConfigState(tuple.sheets[nameof(GameConfigSheet)]);
            states = states.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());

            agentAddr ??= new PrivateKey().Address;
            var avatarAddr = Addresses.GetAvatarAddress(agentAddr.Value, avatarIndex);
            var agentState = new AgentState(agentAddr.Value);
            var avatarState = new AvatarState(
                avatarAddr,
                agentAddr.Value,
                0,
                tableSheets.GetAvatarSheets(),
                avatarAddr.Derive("ranking_map"));
            agentState.avatarAddresses.Add(avatarIndex, avatarAddr);

            var initialStatesWithAvatarStateV2 = states
                .SetAgentState(agentAddr.Value, agentState)
                .SetAvatarState(avatarAddr, avatarState);

            return (
                tableSheets,
                agentAddr.Value,
                avatarAddr,
                initialStatesWithAvatarStateV2);
        }

        public static (IWorld states, Dictionary<string, string> sheets)
            InitializeTableSheets(
                IWorld states,
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
                states = states.SetLegacyState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            return (states, sheets);
        }
    }
}
