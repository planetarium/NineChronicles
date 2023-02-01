namespace Lib9c.Tests
{
    using System;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using static SerializeKeys;
    using State = Lib9c.Tests.Action.State;

    public static class TestUtils
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
                    value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);
            var goldCurrency = Currency.Legacy("NCG", 2, null);
            var goldCurrencyState = new GoldCurrencyState(goldCurrency);
            states = states.SetState(
                goldCurrencyState.address,
                goldCurrencyState.Serialize());

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
                .SetState(agentAddr, agentState.SerializeV2())
                .SetState(avatarAddr, avatarState.SerializeV2())
                .SetState(
                    avatarAddr.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize())
                .SetState(
                    avatarAddr.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize())
                .SetState(
                    avatarAddr.Derive(LegacyQuestListKey),
                    avatarState.questList.Serialize());

            return (
                tableSheets,
                agentAddr,
                avatarAddr,
                initialStatesWithAvatarStateV1,
                initialStatesWithAvatarStateV2);
        }

        public static string CsvLinqWhere(string csv, Func<string, bool> where)
        {
            var after = csv
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(where);
            return string.Join('\n', after);
        }

        public static string CsvLinqSelect(string csv, Func<string, string> select)
        {
            var after = csv
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(select);
            return string.Join('\n', after);
        }
    }
}
