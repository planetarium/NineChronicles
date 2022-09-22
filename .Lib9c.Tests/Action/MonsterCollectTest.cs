namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class MonsterCollectTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _signer;
        private IAccountStateDelta _initialState;

        public MonsterCollectTest()
        {
            Dictionary<string, string> sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            _signer = default;
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);
            _initialState = new State()
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            foreach ((string key, string value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize())
                    .SetState(_signer, new AgentState(_signer).Serialize());
            }
        }

        [Theory]
        [InlineData(500 + 1800 + 7200 + 54000, 1, 2, 1, null, 500 + 1800)]
        [InlineData(500 + 1800 + 7200 + 54000, 1, 2, MonsterCollectionState.LockUpInterval, typeof(MonsterCollectionExistingClaimableException), null)]
        [InlineData(500 + 1800 + 7200 + 54000, 2, 4, 1, null, 500 + 1800 + 7200 + 54000)]
        [InlineData(500 + 1800 + 7200 + 54000 - 1, 2, 4, 1, typeof(InsufficientBalanceException), null)]
        [InlineData(500 + 1800 + 7200 + 54000, 2, 4, MonsterCollectionState.LockUpInterval, typeof(MonsterCollectionExistingClaimableException), null)]
        [InlineData(500 + 1800 + 7200 + 54000, 3, 2, 1, typeof(RequiredBlockIndexException), null)]
        [InlineData(500 + 1800 + 7200 + 54000, 3, 2, MonsterCollectionState.LockUpInterval, typeof(MonsterCollectionExistingClaimableException), null)]
        [InlineData(500 + 1800 + 7200 + 54000, 3, 3, MonsterCollectionState.LockUpInterval, typeof(MonsterCollectionLevelException), null)]
        [InlineData(500 + 1800 + 7200 + 54000, 3, 0, 1, typeof(RequiredBlockIndexException), null)]
        [InlineData(500 + 1800 + 7200 + 54000, 3, 0, MonsterCollectionState.LockUpInterval, typeof(MonsterCollectionExistingClaimableException), 0)]
        [InlineData(500 + 1800 + 7200 + 54000, null, 1, 1, null, 500)]
        [InlineData(500 + 1800 + 7200 + 54000, null, 3, MonsterCollectionState.LockUpInterval, null, 500 + 1800 + 7200)]
        [InlineData(500 + 1800 + 7200 + 54000, null, -1, 1, typeof(MonsterCollectionLevelException), null)]
        [InlineData(500 + 1800 + 7200 + 54000, null, 100, 1, typeof(MonsterCollectionLevelException), null)]
        [InlineData(500 + 1800 + 7200 + 54000, null, 0, 1, null, 0)]
        public void Execute(int balance, int? prevLevel, int level, long blockIndex, Type exc, int? expectedStakings)
        {
            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(_signer, 0);
            Currency currency = _initialState.GetGoldCurrency();
            FungibleAssetValue balanceFav = currency * balance;
            FungibleAssetValue staked = currency * 0;
            if (prevLevel is { } prevLevelNotNull)
            {
                List<MonsterCollectionRewardSheet.RewardInfo> rewards = _tableSheets.MonsterCollectionRewardSheet[prevLevelNotNull].Rewards;
                var prevMonsterCollectionState = new MonsterCollectionState(
                    address: monsterCollectionAddress,
                    level: prevLevelNotNull,
                    blockIndex: 0,
                    monsterCollectionRewardSheet: _tableSheets.MonsterCollectionRewardSheet
                );
                _initialState = _initialState.SetState(monsterCollectionAddress, prevMonsterCollectionState.Serialize());
                for (int i = 0; i < prevLevel; i++)
                {
                    MonsterCollectionSheet.Row row = _tableSheets.MonsterCollectionSheet[i + 1];
                    staked += row.RequiredGold * currency;
                    _initialState = _initialState.MintAsset(monsterCollectionAddress, row.RequiredGold * currency);
                }
            }

            balanceFav -= staked;

            _initialState = _initialState.MintAsset(_signer, balanceFav);
            var action = new MonsterCollect
            {
                level = level,
            };

            if (exc is { } excType)
            {
                Assert.Throws(excType, () => action.Execute(new ActionContext
                {
                    PreviousStates = _initialState,
                    Signer = _signer,
                    BlockIndex = blockIndex,
                }));
            }
            else
            {
                IAccountStateDelta nextState = action.Execute(new ActionContext
                {
                    PreviousStates = _initialState,
                    Signer = _signer,
                    BlockIndex = blockIndex,
                });

                Assert.Equal(expectedStakings * currency, nextState.GetBalance(monsterCollectionAddress, currency));
                Assert.Equal(balanceFav + staked - (expectedStakings * currency), nextState.GetBalance(_signer, currency));

                if (level == 0)
                {
                    Assert.Equal(Null.Value, nextState.GetState(monsterCollectionAddress));
                }
                else
                {
                    var nextMonsterCollectionState = new MonsterCollectionState((Dictionary)nextState.GetState(monsterCollectionAddress));
                    Assert.Equal(level, nextMonsterCollectionState.Level);
                    Assert.Equal(blockIndex, nextMonsterCollectionState.StartedBlockIndex);
                    Assert.Equal(0, nextMonsterCollectionState.ReceivedBlockIndex);
                    Assert.Equal(0, nextMonsterCollectionState.ExpiredBlockIndex);
                }
            }
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            MonsterCollect action = new MonsterCollect
            {
                level = 1,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
            {
                PreviousStates = new State(),
                Signer = _signer,
                BlockIndex = 1,
            }));
        }

        [Fact]
        public void Execute_Throw_InsufficientBalanceException()
        {
            MonsterCollect action = new MonsterCollect
            {
                level = 1,
            };

            Assert.Throws<InsufficientBalanceException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signer,
                BlockIndex = 1,
            }));
        }

        [Fact]
        public void Execute_Throw_InvalidOperationException()
        {
            var stakeAddress = StakeState.DeriveAddress(_signer);
            var states = _initialState.SetState(
                stakeAddress,
                new StakeState(stakeAddress, 0).SerializeV2());
            MonsterCollect action = new MonsterCollect
            {
                level = 1,
            };

            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signer,
                BlockIndex = 1,
            }));
        }

        [Fact]
        public void Rehearsal()
        {
            MonsterCollect action = new MonsterCollect
            {
                level = 1,
            };
            IAccountStateDelta nextState = action.Execute(new ActionContext
            {
                PreviousStates = new State(),
                Signer = _signer,
                Rehearsal = true,
            });

            List<Address> updatedAddresses = new List<Address>()
            {
                _signer,
                MonsterCollectionState.DeriveAddress(_signer, 0),
                MonsterCollectionState.DeriveAddress(_signer, 1),
                MonsterCollectionState.DeriveAddress(_signer, 2),
                MonsterCollectionState.DeriveAddress(_signer, 3),
            };

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }
    }
}
