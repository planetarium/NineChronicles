using Lib9c.Tests;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lib9c.DevExtensions.Action;
using Lib9c.Tests.Action;
using Libplanet;
using Libplanet.Action;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Helper;
using Nekoyume.Model.Faucet;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using static Lib9c.SerializeKeys;

namespace Lib9c.DevExtensions.Tests.Action
{
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

            _initialState = new Lib9c.Tests.Action.State();
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
                    .SetState(
                        _avatarAddress.Derive(LegacyInventoryKey),
                        avatarState.inventory.Serialize()
                    )
                    .SetState(
                        _avatarAddress.Derive(LegacyWorldInformationKey),
                        avatarState.worldInformation.Serialize()
                    )
                    .SetState(
                        _avatarAddress.Derive(LegacyQuestListKey),
                        avatarState.questList.Serialize()
                    )
                    .SetState(
                        _avatarAddress, avatarState.Serialize()
                    )
                ;
        }

        [Theory]
        [ClassData(typeof(FaucetRuneInfoGenerator))]
        public void Execute_FaucetRune(List<FaucetRuneInfo> faucetRuneInfos)
        {
            var action = new FaucetRune
            {
                AvatarAddress = _avatarAddress,
                FaucetRuneInfos = faucetRuneInfos,
            };
            var states = action.Execute(new ActionContext { PreviousStates = _initialState });
            foreach (var rune in faucetRuneInfos)
            {
                var expectedRune = RuneHelper.ToCurrency(
                    _runeSheet.OrderedList.First(r => r.Id == rune.RuneId),
                    0,
                    null
                );
                Assert.Equal(
                    rune.Amount * expectedRune,
                    states.GetBalance(_avatarAddress, expectedRune)
                );
            }
        }

        private class FaucetRuneInfoGenerator : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new List<object[]>
            {
                new object[]
                {
                    new List<FaucetRuneInfo>
                    {
                        new FaucetRuneInfo(10001, 10),
                    },
                },
                new object[]
                {
                    new List<FaucetRuneInfo>
                    {
                        new FaucetRuneInfo(10001, 10),
                        new FaucetRuneInfo(30001, 10),
                    },
                },
                new object[]
                {
                    new List<FaucetRuneInfo>
                    {
                        new FaucetRuneInfo(10001, 10),
                        new FaucetRuneInfo(10002, 10),
                        new FaucetRuneInfo(30001, 10),
                    },
                },
            };

            public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
