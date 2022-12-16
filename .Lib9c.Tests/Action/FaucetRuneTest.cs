namespace Lib9c.Tests.Action
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;

    public class FaucetRuneTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Address _avatarAddress;
        private readonly RuneSheet _runeSheet;

        public FaucetRuneTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new State();
            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);
            _runeSheet = _initialState.GetSheet<RuneSheet>();

            Address agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentAddress);
            var avatarState = new AvatarState(
                _avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                new PrivateKey().ToAddress()
            );
            agentState.avatarAddresses.Add(0, _avatarAddress);

            _initialState = _initialState
                    .SetState(agentAddress, agentState.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                    .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                    .SetState(_avatarAddress, avatarState.Serialize())
                ;
        }

        [Theory]
        [ClassData(typeof(FaucetRuneGenerator))]
        public void Execute_FaucetRune(Dictionary<int, int> faucetRune)
        {
            var action = new FaucetRune(_avatarAddress, faucetRune);
            var states = action.Execute(new ActionContext { PreviousStates = _initialState });
            foreach (var (runeId, runeCount) in faucetRune)
            {
                var expectedRune = RuneHelper.ToCurrency(
                    _runeSheet.OrderedList.First(r => r.Id == runeId),
                    0,
                    null
                );
                Assert.Equal(runeCount * expectedRune, states.GetBalance(_avatarAddress, expectedRune)
                );
            }
        }

        private class FaucetRuneGenerator : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new List<object[]>
            {
                new object[]
                {
                    new Dictionary<int, int>
                    {
                        { 10001, 10 },
                    },
                },
                new object[]
                {
                    new Dictionary<int, int>
                    {
                        { 10001, 10 }, { 30001, 10 },
                    },
                },
                new object[]
                {
                    new Dictionary<int, int>
                    {
                        { 10001, 10 }, { 10002, 10 }, { 30001, 10 },
                    },
                },
            };

            public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
