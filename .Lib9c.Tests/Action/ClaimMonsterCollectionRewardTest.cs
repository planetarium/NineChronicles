namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;

    public class ClaimMonsterCollectionRewardTest
    {
        private readonly Address _signer;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private IAccountStateDelta _state;

        public ClaimMonsterCollectionRewardTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _signer = default;
            _avatarAddress = _signer.Derive("avatar");
            _state = new State();
            Dictionary<string, string> sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            var rankingMapAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_signer);
            var avatarState = new AvatarState(
                _avatarAddress,
                _signer,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            var currency = new Currency("NCG", 2, minters: null);
            var goldCurrencyState = new GoldCurrencyState(currency);

            _state = _state
                .SetState(_signer, agentState.Serialize())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            foreach ((string key, string value) in sheets)
            {
                _state = _state
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(MonsterCollectionState.RewardInterval, 1, 0)]
        [InlineData(MonsterCollectionState.RewardInterval * 2, 2, MonsterCollectionState.RewardInterval)]
        [InlineData(MonsterCollectionState.RewardInterval * 3, 3, MonsterCollectionState.RewardInterval)]
        [InlineData(MonsterCollectionState.RewardInterval * 4, 4, MonsterCollectionState.RewardInterval)]
        [InlineData(MonsterCollectionState.RewardInterval * 5, 5, MonsterCollectionState.RewardInterval)]
        [InlineData(MonsterCollectionState.RewardInterval * 6, 6, MonsterCollectionState.RewardInterval)]
        [InlineData(MonsterCollectionState.RewardInterval * 7, 7, MonsterCollectionState.RewardInterval)]
        public void Execute(long blockIndex, int collectionLevel, long receivedBlockIndex)
        {
            Address collectionAddress = MonsterCollectionState.DeriveAddress(_signer, 0);
            MonsterCollectionState monsterCollectionState = new MonsterCollectionState(collectionAddress, collectionLevel, 0);
            monsterCollectionState.Receive(receivedBlockIndex);
            AvatarState prevAvatarState = _state.GetAvatarStateV2(_avatarAddress);
            Assert.Empty(prevAvatarState.mailBox);

            Currency currency = _state.GetGoldCurrency();

            _state = _state.SetState(collectionAddress, monsterCollectionState.SerializeV2());

            Assert.Equal(0, _state.GetAgentState(_signer).MonsterCollectionRound);
            Assert.Equal(0 * currency, _state.GetBalance(_signer, currency));
            Assert.Equal(0 * currency, _state.GetBalance(collectionAddress, currency));

            ClaimMonsterCollectionReward action = new ClaimMonsterCollectionReward
            {
                avatarAddress = _avatarAddress,
            };

            IAccountStateDelta nextState = action.Execute(new ActionContext
            {
                PreviousStates = _state,
                Signer = _signer,
                BlockIndex = blockIndex,
                Random = new TestRandom(),
            });

            MonsterCollectionState nextMonsterCollectionState = new MonsterCollectionState((Dictionary)nextState.GetState(collectionAddress));
            Assert.Equal(0, nextMonsterCollectionState.RewardLevel);

            AvatarState nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
            Assert.Single(nextAvatarState.mailBox);
            Mail mail = nextAvatarState.mailBox.First();
            Assert.IsType<MonsterCollectionMail>(mail);
            MonsterCollectionMail monsterCollectionMail = (MonsterCollectionMail)mail;
            Assert.IsType<MonsterCollectionResult>(monsterCollectionMail.attachment);
            MonsterCollectionResult result = (MonsterCollectionResult)monsterCollectionMail.attachment;
            Assert.Equal(result.id, mail.id);
            Assert.Equal(0, nextMonsterCollectionState.StartedBlockIndex);
            Assert.Equal(blockIndex, nextMonsterCollectionState.ReceivedBlockIndex);
            Assert.Equal(0 * currency, nextState.GetBalance(_signer, currency));
            Assert.Equal(0, nextState.GetAgentState(_signer).MonsterCollectionRound);

            foreach (var rewardInfo in result.rewards)
            {
                Assert.True(nextAvatarState.inventory.HasItem(rewardInfo.ItemId, rewardInfo.Quantity));
            }
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException_AgentState()
        {
            ClaimMonsterCollectionReward action = new ClaimMonsterCollectionReward
            {
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = _state,
                    Signer = new PrivateKey().ToAddress(),
                    BlockIndex = 0,
                })
            );
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException_MonsterCollectionState()
        {
            ClaimMonsterCollectionReward action = new ClaimMonsterCollectionReward
            {
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = _state,
                    Signer = _signer,
                    BlockIndex = 0,
                })
            );
        }

        [Fact]
        public void Execute_Throw_RequiredBlockIndexException()
        {
            Address collectionAddress = MonsterCollectionState.DeriveAddress(_signer, 0);
            MonsterCollectionState monsterCollectionState = new MonsterCollectionState(collectionAddress, 1, 0);
            _state = _state.SetState(collectionAddress, monsterCollectionState.SerializeV2());

            ClaimMonsterCollectionReward action = new ClaimMonsterCollectionReward
            {
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<RequiredBlockIndexException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = _state,
                    Signer = _signer,
                    BlockIndex = 0,
                })
            );
        }

        [Fact]
        public void Rehearsal()
        {
            ClaimMonsterCollectionReward action = new ClaimMonsterCollectionReward
            {
                avatarAddress = _avatarAddress,
            };

            IAccountStateDelta nextState = action.Execute(new ActionContext
                {
                    PreviousStates = new State(),
                    Signer = _signer,
                    BlockIndex = 0,
                    Rehearsal = true,
                }
            );

            List<Address> updatedAddresses = new List<Address>
            {
                _avatarAddress,
                _avatarAddress.Derive(LegacyInventoryKey),
                MonsterCollectionState.DeriveAddress(_signer, 0),
            };
            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }
    }
}
