namespace Lib9c.Tests.Action
{
    using System.Collections;
    using System.Collections.Generic;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;

    public class MigrateMonsterCollectionTest
    {
        private readonly Address _signer;
        private readonly IAccountStateDelta _state;

        public MigrateMonsterCollectionTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _signer = default;
            var avatarAddress = _signer.Derive("avatar");
            _state = new State();
            Dictionary<string, string> sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var rankingMapAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_signer);
            var avatarState = new AvatarState(
                avatarAddress,
                _signer,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress);
            agentState.avatarAddresses[0] = avatarAddress;

            var currency = new Currency("NCG", 2, minters: null);
            var goldCurrencyState = new GoldCurrencyState(currency);

            _state = _state
                .SetState(_signer, agentState.Serialize())
                .SetState(avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            foreach ((string key, string value) in sheets)
            {
                _state = _state
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [ClassData(typeof(ExecuteFixture))]
        public void Execute(int collectionLevel, long claimBlockIndex, long stakedAmount)
        {
            Address collectionAddress = MonsterCollectionState.DeriveAddress(_signer, 0);
            var monsterCollectionState = new MonsterCollectionState(collectionAddress, collectionLevel, 0);
            Currency currency = _state.GetGoldCurrency();

            var states = _state
                .SetState(collectionAddress, monsterCollectionState.Serialize())
                .MintAsset(monsterCollectionState.address, stakedAmount * currency);

            Assert.Equal(0, states.GetAgentState(_signer).MonsterCollectionRound);
            Assert.Equal(0 * currency, states.GetBalance(_signer, currency));
            Assert.Equal(stakedAmount * currency, states.GetBalance(collectionAddress, currency));

            MigrateMonsterCollection action = new MigrateMonsterCollection();
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signer,
                BlockIndex = claimBlockIndex,
                Random = new TestRandom(),
            });

            Assert.True(states.TryGetStakeState(_signer, out StakeState stakeState));
            Assert.Equal(
                0 * currency,
                states.GetBalance(monsterCollectionState.address, currency));
            Assert.Equal(stakedAmount * currency, states.GetBalance(stakeState.address, currency));
            Assert.Equal(monsterCollectionState.ReceivedBlockIndex, stakeState.ReceivedBlockIndex);
        }

        [Fact]
        public void Serialization()
        {
            var action = new MigrateMonsterCollection();
            var deserialized = new MigrateMonsterCollection();
            deserialized.LoadPlainValue(action.PlainValue);
            Assert.Equal(action.PlainValue, deserialized.PlainValue);
        }

        [Fact]
        public void CannotBePolymorphicAction()
        {
            Assert.Throws<MissingActionTypeException>(() =>
            {
                PolymorphicAction<ActionBase> action = new MigrateMonsterCollection();
            });
        }

        private class ExecuteFixture : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new List<object[]>
            {
                new object[]
                {
                    1,
                    MonsterCollectionState.RewardInterval,
                    500,
                },
                new object[]
                {
                    2,
                    MonsterCollectionState.RewardInterval,
                    2300,
                },
                new object[]
                {
                    3,
                    MonsterCollectionState.RewardInterval,
                    9500,
                },
                new object[]
                {
                    4,
                    MonsterCollectionState.RewardInterval,
                    63500,
                },
                new object[]
                {
                    5,
                    MonsterCollectionState.RewardInterval,
                    333500,
                },
                new object[]
                {
                    6,
                    MonsterCollectionState.RewardInterval,
                    813500,
                },
                new object[]
                {
                    7,
                    MonsterCollectionState.RewardInterval,
                    2313500,
                },
            };

            public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
        }
    }
}
