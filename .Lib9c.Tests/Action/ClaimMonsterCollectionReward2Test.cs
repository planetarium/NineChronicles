namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections;
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
    using static Lib9c.SerializeKeys;
    using static Nekoyume.Model.Item.Inventory;

    public class ClaimMonsterCollectionReward2Test
    {
        private readonly Address _signer;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private IAccountStateDelta _state;

        public ClaimMonsterCollectionReward2Test(ITestOutputHelper outputHelper)
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

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
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
        [ClassData(typeof(ExecuteFixture))]
        public void Execute(int collectionLevel, long claimBlockIndex, long? receivedBlockIndex, (int, int)[] expectedRewards, Type exc)
        {
            Address collectionAddress = MonsterCollectionState.DeriveAddress(_signer, 0);
            var monsterCollectionState = new MonsterCollectionState(collectionAddress, collectionLevel, 0);
            if (receivedBlockIndex is { } receivedBlockIndexNotNull)
            {
                monsterCollectionState.Claim(receivedBlockIndexNotNull);
            }

            AvatarState prevAvatarState = _state.GetAvatarStateV2(_avatarAddress);
            Assert.Empty(prevAvatarState.mailBox);

            Currency currency = _state.GetGoldCurrency();

            _state = _state.SetState(collectionAddress, monsterCollectionState.Serialize());

            Assert.Equal(0, _state.GetAgentState(_signer).MonsterCollectionRound);
            Assert.Equal(0 * currency, _state.GetBalance(_signer, currency));
            Assert.Equal(0 * currency, _state.GetBalance(collectionAddress, currency));

            ClaimMonsterCollectionReward2 action = new ClaimMonsterCollectionReward2
            {
                avatarAddress = _avatarAddress,
            };

            if (exc is { })
            {
                Assert.Throws(exc, () =>
                {
                    action.Execute(new ActionContext
                    {
                        PreviousStates = _state,
                        Signer = _signer,
                        BlockIndex = claimBlockIndex,
                        Random = new TestRandom(),
                    });
                });
            }
            else
            {
                IAccountStateDelta nextState = action.Execute(new ActionContext
                {
                    PreviousStates = _state,
                    Signer = _signer,
                    BlockIndex = claimBlockIndex,
                    Random = new TestRandom(),
                });

                var nextMonsterCollectionState = new MonsterCollectionState(
                    (Dictionary)nextState.GetState(collectionAddress)
                );
                Assert.Equal(0, nextMonsterCollectionState.RewardLevel);

                AvatarState nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
                Assert.Single(nextAvatarState.mailBox);
                Mail mail = nextAvatarState.mailBox.First();
                MonsterCollectionMail monsterCollectionMail = Assert.IsType<MonsterCollectionMail>(mail);
                MonsterCollectionResult result =
                    Assert.IsType<MonsterCollectionResult>(monsterCollectionMail.attachment);
                Assert.Equal(result.id, mail.id);
                Assert.Equal(0, nextMonsterCollectionState.StartedBlockIndex);
                Assert.Equal(claimBlockIndex, nextMonsterCollectionState.ReceivedBlockIndex);
                Assert.Equal(0 * currency, nextState.GetBalance(_signer, currency));
                Assert.Equal(0, nextState.GetAgentState(_signer).MonsterCollectionRound);

                foreach ((int id, int quantity) in expectedRewards)
                {
                    Assert.True(nextAvatarState.inventory.TryGetItem(id, out Item item));
                    Assert.Equal(quantity, item.count);
                }
            }
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException_AgentState()
        {
            ClaimMonsterCollectionReward2 action = new ClaimMonsterCollectionReward2
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
            ClaimMonsterCollectionReward2 action = new ClaimMonsterCollectionReward2
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
            var monsterCollectionState = new MonsterCollectionState(collectionAddress, 1, 0);
            _state = _state.SetState(collectionAddress, monsterCollectionState.Serialize());

            ClaimMonsterCollectionReward2 action = new ClaimMonsterCollectionReward2
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
            ClaimMonsterCollectionReward2 action = new ClaimMonsterCollectionReward2
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
                _avatarAddress.Derive(LegacyWorldInformationKey),
                _avatarAddress.Derive(LegacyQuestListKey),
                MonsterCollectionState.DeriveAddress(_signer, 0),
                MonsterCollectionState.DeriveAddress(_signer, 1),
                MonsterCollectionState.DeriveAddress(_signer, 2),
                MonsterCollectionState.DeriveAddress(_signer, 3),
            };
            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }

        private class ExecuteFixture : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new List<object[]>
            {
                new object[]
                {
                    1,
                    MonsterCollectionState.RewardInterval,
                    null,
                    new (int, int)[]
                    {
                        (400000, 80),
                        (500000, 1),
                    },
                    null,
                },
                new object[]
                {
                    2,
                    MonsterCollectionState.RewardInterval,
                    null,
                    new (int, int)[]
                    {
                        (400000, 265),
                        (500000, 2),
                    },
                    null,
                },
                new object[]
                {
                    3,
                    MonsterCollectionState.RewardInterval,
                    null,
                    new (int, int)[]
                    {
                        (400000, 1265),
                        (500000, 5),
                    },
                    null,
                },
                new object[]
                {
                    4,
                    MonsterCollectionState.RewardInterval,
                    null,
                    new (int, int)[]
                    {
                        (400000, 8465),
                        (500000, 31),
                    },
                    null,
                },
                new object[]
                {
                    5,
                    MonsterCollectionState.RewardInterval,
                    null,
                    new (int, int)[]
                    {
                        (400000, 45965),
                        (500000, 161),
                    },
                    null,
                },
                new object[]
                {
                    6,
                    MonsterCollectionState.RewardInterval,
                    null,
                    new (int, int)[]
                    {
                        (400000, 120965),
                        (500000, 361),
                    },
                    null,
                },
                new object[]
                {
                    7,
                    MonsterCollectionState.RewardInterval,
                    null,
                    new (int, int)[]
                    {
                        (400000, 350965),
                        (500000, 1121),
                    },
                    null,
                },
                new object[]
                {
                    1,
                    MonsterCollectionState.RewardInterval * 2,
                    null,
                    new (int, int)[]
                    {
                        (400000, 80 * 2),
                        (500000, 1 * 2),
                    },
                    null,
                },
                new object[]
                {
                    2,
                    MonsterCollectionState.RewardInterval * 2,
                    null,
                    new (int, int)[]
                    {
                        (400000, 265 * 2),
                        (500000, 2 * 2),
                    },
                    null,
                },
                new object[]
                {
                    1,
                    MonsterCollectionState.RewardInterval * 2,
                    MonsterCollectionState.RewardInterval * 2 - 1,
                    new (int, int)[]
                    {
                        (400000, 80),
                        (500000, 1),
                    },
                    null,
                },
                new object[]
                {
                    1,
                    1,
                    null,
                    new (int, int)[] { },
                    typeof(RequiredBlockIndexException),
                },
                new object[]
                {
                    1,
                    MonsterCollectionState.RewardInterval + 1,
                    MonsterCollectionState.RewardInterval,
                    new (int, int)[] { },
                    typeof(RequiredBlockIndexException),
                },
            };

            public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
        }
    }
}
