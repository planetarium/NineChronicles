namespace Lib9c.Tests.Util
{
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using State = Lib9c.Tests.Action.State;
    using StateExtensions = Nekoyume.Model.State.StateExtensions;

    public static class InitializeUtil
    {
        public static (
            TableSheets tableSheets,
            Address agentAddress,
            Address avatarAddress,
            IAccountStateDelta initialStatesWithAvatarStateV1,
            IAccountStateDelta initialStatesWithAvatarStateV2) InitializeStates()
        {
            IAccountStateDelta states = new State();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                states = states.SetState(
                    Addresses.TableSheet.Derive(key),
                    StateExtensions.Serialize(value));
            }

            var tableSheets = new TableSheets(sheets);
            var goldCurrency = Currency.Legacy("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(goldCurrency);
            states = states.SetState(
                goldCurrencyState.address,
                goldCurrencyState.Serialize());

            var gameConfigState = new GameConfigState(sheets[nameof(GameConfigSheet)]);
            states = states.SetState(gameConfigState.address, gameConfigState.Serialize());

            var agentAddr = new PrivateKey().ToAddress();
            var avatarAddr = Addresses.GetAvatarAddress(agentAddr, 0);
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
