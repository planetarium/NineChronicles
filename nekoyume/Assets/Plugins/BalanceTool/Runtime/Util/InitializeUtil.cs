using Lib9c;
using Nekoyume.Game;
using Nekoyume.Helper;

namespace BalanceTool.Runtime.Util
{
    namespace Lib9c.Tests.Util
    {
        using System.Collections.Immutable;
        using Libplanet;
        using Libplanet.Action;
        using Libplanet.Assets;
        using Libplanet.Crypto;
        using Nekoyume;
        using Nekoyume.Action;
        using Nekoyume.Model.State;
        using Nekoyume.TableData;
        using State = Lib9c.Tests.Action.State;

        public static class InitializeUtil
        {
            public static (
                TableSheets tableSheets,
                Address agentAddr,
                Address avatarAddr,
                IAccountStateDelta initialStatesWithAvatarStateV1,
                IAccountStateDelta initialStatesWithAvatarStateV2
                ) InitializeStates(
                    Address? adminAddr = null,
                    string tableCsvPath = null,
                    int avatarIndex = 0)
            {
                adminAddr ??= new PrivateKey().ToAddress();
                var states = new State().SetState(
                    Addresses.Admin,
                    new AdminState(adminAddr.Value, long.MaxValue).Serialize());
#if UNITY_EDITOR
                var sheets = TableSheetsHelper.ImportSheets();
#else
                var sheets = TableSheetsImporter.ImportSheets(tableCsvPath);
#endif
                foreach (var (key, value) in sheets)
                {
                    states = states.SetState(
                        Addresses.TableSheet.Derive(key),
                        value.Serialize());
                }

                var tableSheets = new TableSheets(sheets);
                var goldCurrency = Currency.Legacy(
                    "NCG",
                    2,
                    minters: new[] { adminAddr.Value }.ToImmutableHashSet());
                var goldCurrencyState = new GoldCurrencyState(goldCurrency);
                states = states
                    .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                    .MintAsset(goldCurrencyState.address, goldCurrency * 1_000_000_000);

                var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
                states = states.SetState(gameConfigState.address, gameConfigState.Serialize());

                var agentAddr = new PrivateKey().ToAddress();
                var avatarAddr = Addresses.GetAvatarAddress(agentAddr, avatarIndex);
                var agentState = new AgentState(agentAddr);
                var avatarState = new AvatarState(
                    avatarAddr,
                    agentAddr,
                    0,
                    tableSheets.GetAvatarSheets(),
                    new GameConfigState(),
                    avatarAddr.Derive("ranking_map"));
                agentState.avatarAddresses.Add(0, avatarAddr);

                var initialStatesWithAvatarStateV1 = states
                    .SetState(agentAddr, agentState.Serialize())
                    .SetState(avatarAddr, avatarState.Serialize());
                var initialStatesWithAvatarStateV2 = states
                    .SetState(agentAddr, agentState.Serialize())
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
                    agentAddr,
                    avatarAddr,
                    initialStatesWithAvatarStateV1,
                    initialStatesWithAvatarStateV2);
            }
        }
    }
}
