namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class ClaimMonsterCollectionReward0Test
    {
        private readonly Address _signer;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private IAccountStateDelta _state;

        public ClaimMonsterCollectionReward0Test()
        {
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

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);

            _state = _state
                .SetState(_signer, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            foreach ((string key, string value) in sheets)
            {
                _state = _state
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(1, 0, 1)]
        [InlineData(2, 0, 2)]
        [InlineData(2, 1, 3)]
        [InlineData(3, 0, 4)]
        [InlineData(3, 1, 5)]
        [InlineData(3, 2, 6)]
        [InlineData(4, 0, 7)]
        [InlineData(4, 1, 4)]
        [InlineData(4, 2, 5)]
        [InlineData(4, 3, 6)]
        public void Execute(int rewardLevel, int prevRewardLevel, int collectionLevel)
        {
            Address collectionAddress = MonsterCollectionState0.DeriveAddress(_signer, 0);
            List<MonsterCollectionRewardSheet.RewardInfo> rewards = _tableSheets.MonsterCollectionRewardSheet[1].Rewards;
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(collectionAddress, 1, 0, _tableSheets.MonsterCollectionRewardSheet);
            for (int i = 0; i < prevRewardLevel; i++)
            {
                int level = i + 1;
                MonsterCollectionResult result = new MonsterCollectionResult(Guid.NewGuid(), _avatarAddress, rewards);
                monsterCollectionState.UpdateRewardMap(level, result, i * MonsterCollectionState0.RewardInterval);
            }

            List<MonsterCollectionRewardSheet.RewardInfo> collectionRewards = _tableSheets.MonsterCollectionRewardSheet[collectionLevel].Rewards;
            monsterCollectionState.Update(collectionLevel, rewardLevel, _tableSheets.MonsterCollectionRewardSheet);
            for (long i = rewardLevel; i < 4; i++)
            {
                Assert.Equal(collectionRewards, monsterCollectionState.RewardLevelMap[i + 1]);
            }

            Dictionary<int, int> rewardExpectedMap = new Dictionary<int, int>();
            foreach (var (key, value) in monsterCollectionState.RewardLevelMap)
            {
                if (monsterCollectionState.RewardMap.ContainsKey(key) || key > rewardLevel)
                {
                    continue;
                }

                foreach (var info in value)
                {
                    if (rewardExpectedMap.ContainsKey(info.ItemId))
                    {
                        rewardExpectedMap[info.ItemId] += info.Quantity;
                    }
                    else
                    {
                        rewardExpectedMap[info.ItemId] = info.Quantity;
                    }
                }
            }

            AvatarState prevAvatarState = _state.GetAvatarState(_avatarAddress);
            Assert.Empty(prevAvatarState.mailBox);

            Currency currency = _state.GetGoldCurrency();
            int collectionRound = _state.GetAgentState(_signer).MonsterCollectionRound;

            _state = _state
                .SetState(collectionAddress, monsterCollectionState.Serialize());

            FungibleAssetValue balance = 0 * currency;
            if (rewardLevel == 4)
            {
                foreach (var row in _tableSheets.MonsterCollectionSheet)
                {
                    if (row.Level <= collectionLevel)
                    {
                        balance += row.RequiredGold * currency;
                    }
                }

                collectionRound += 1;
                _state = _state
                    .MintAsset(collectionAddress, balance);
            }

            Assert.Equal(prevRewardLevel, monsterCollectionState.RewardLevel);
            Assert.Equal(0, _state.GetAgentState(_signer).MonsterCollectionRound);

            ClaimMonsterCollectionReward0 action = new ClaimMonsterCollectionReward0
            {
                avatarAddress = _avatarAddress,
                collectionRound = 0,
            };

            IAccountStateDelta nextState = action.Execute(new ActionContext
            {
                PreviousStates = _state,
                Signer = _signer,
                BlockIndex = rewardLevel * MonsterCollectionState0.RewardInterval,
                Random = new TestRandom(),
            });

            MonsterCollectionState0 nextMonsterCollectionState = new MonsterCollectionState0((Dictionary)nextState.GetState(collectionAddress));
            Assert.Equal(rewardLevel, nextMonsterCollectionState.RewardLevel);

            AvatarState nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            foreach (var (itemId, qty) in rewardExpectedMap)
            {
                Assert.True(nextAvatarState.inventory.HasItem(itemId, qty));
            }

            Assert.Equal(rewardLevel - prevRewardLevel, nextAvatarState.mailBox.Count);
            Assert.All(nextAvatarState.mailBox, mail =>
            {
                Assert.IsType<MonsterCollectionMail>(mail);
                MonsterCollectionMail monsterCollectionMail = (MonsterCollectionMail)mail;
                Assert.IsType<MonsterCollectionResult>(monsterCollectionMail.attachment);
                MonsterCollectionResult result = (MonsterCollectionResult)monsterCollectionMail.attachment;
                Assert.Equal(result.id, mail.id);
            });

            for (int i = 0; i < nextMonsterCollectionState.RewardLevel; i++)
            {
                int level = i + 1;
                List<MonsterCollectionRewardSheet.RewardInfo> rewardInfos = _tableSheets.MonsterCollectionRewardSheet[collectionLevel].Rewards;
                Assert.Contains(level, nextMonsterCollectionState.RewardMap.Keys);
                Assert.Equal(_avatarAddress, nextMonsterCollectionState.RewardMap[level].avatarAddress);
            }

            Assert.Equal(0 * currency, nextState.GetBalance(collectionAddress, currency));
            Assert.Equal(balance, nextState.GetBalance(_signer, currency));
            Assert.Equal(collectionRound, nextState.GetAgentState(_signer).MonsterCollectionRound);
            Assert.Equal(nextMonsterCollectionState.End, rewardLevel == 4);
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException_AgentState()
        {
            ClaimMonsterCollectionReward0 action = new ClaimMonsterCollectionReward0
            {
                avatarAddress = _avatarAddress,
                collectionRound = 0,
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
            ClaimMonsterCollectionReward0 action = new ClaimMonsterCollectionReward0
            {
                avatarAddress = _avatarAddress,
                collectionRound = 1,
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
        public void Execute_Throw_MonsterCollectionExpiredException()
        {
            Address collectionAddress = MonsterCollectionState0.DeriveAddress(_signer, 0);
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(collectionAddress, 1, 0, _tableSheets.MonsterCollectionRewardSheet);
            List<MonsterCollectionRewardSheet.RewardInfo> rewards = _tableSheets.MonsterCollectionRewardSheet[4].Rewards;
            MonsterCollectionResult result = new MonsterCollectionResult(Guid.NewGuid(), _avatarAddress, rewards);
            monsterCollectionState.UpdateRewardMap(4, result, 0);
            _state = _state.SetState(collectionAddress, monsterCollectionState.Serialize());

            ClaimMonsterCollectionReward0 action = new ClaimMonsterCollectionReward0
            {
                avatarAddress = _avatarAddress,
                collectionRound = 0,
            };

            Assert.Throws<MonsterCollectionExpiredException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = _state,
                    Signer = _signer,
                    BlockIndex = 0,
                })
            );
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, MonsterCollectionState0.RewardInterval - 1)]
        public void Execute_Throw_RequiredBlockIndexException(long startedBlockIndex, long blockIndex)
        {
            Address collectionAddress = MonsterCollectionState0.DeriveAddress(_signer, 0);
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(collectionAddress, 1, startedBlockIndex, _tableSheets.MonsterCollectionRewardSheet);
            List<MonsterCollectionRewardSheet.RewardInfo> rewards = _tableSheets.MonsterCollectionRewardSheet[1].Rewards;
            MonsterCollectionResult result = new MonsterCollectionResult(Guid.NewGuid(), _avatarAddress, rewards);
            monsterCollectionState.UpdateRewardMap(1, result, 0);

            _state = _state.SetState(collectionAddress, monsterCollectionState.Serialize());

            ClaimMonsterCollectionReward0 action = new ClaimMonsterCollectionReward0
            {
                avatarAddress = _avatarAddress,
                collectionRound = 0,
            };

            Assert.Throws<RequiredBlockIndexException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = _state,
                    Signer = _signer,
                    BlockIndex = blockIndex,
                })
            );
        }

        [Fact]
        public void Execute_Throw_InsufficientBalanceException()
        {
            Address collectionAddress = MonsterCollectionState0.DeriveAddress(_signer, 0);
            MonsterCollectionState0 monsterCollectionState = new MonsterCollectionState0(collectionAddress, 1, 0, _tableSheets.MonsterCollectionRewardSheet);

            _state = _state.SetState(collectionAddress, monsterCollectionState.Serialize());

            ClaimMonsterCollectionReward0 action = new ClaimMonsterCollectionReward0
            {
                avatarAddress = _avatarAddress,
                collectionRound = 0,
            };

            Assert.Throws<InsufficientBalanceException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = _state,
                    Signer = _signer,
                    BlockIndex = MonsterCollectionState0.ExpirationIndex,
                    Random = new TestRandom(),
                })
            );
        }
    }
}
