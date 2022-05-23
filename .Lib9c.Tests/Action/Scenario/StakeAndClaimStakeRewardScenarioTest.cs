namespace Lib9c.Tests.Action.Scenario
{
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;
    using State = Lib9c.Tests.Action.State;

    public class StakeAndClaimStakeRewardScenarioTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Currency _currency;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly TableSheets _tableSheets;
        private readonly Address _signerAddress;
        private readonly Address _avatarAddress;

        public StakeAndClaimStakeRewardScenarioTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new State();

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);

            _currency = new Currency("NCG", 2, minters: null);
            _goldCurrencyState = new GoldCurrencyState(_currency);

            _signerAddress = new PrivateKey().ToAddress();
            var stakeStateAddress = StakeState.DeriveAddress(_signerAddress);
            var agentState = new AgentState(_signerAddress);
            _avatarAddress = new PrivateKey().ToAddress();
            var rankingMapAddress = _avatarAddress.Derive("ranking_map");
            agentState.avatarAddresses.Add(0, _avatarAddress);
            var avatarState = new AvatarState(
                _avatarAddress,
                _signerAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                rankingMapAddress
            )
            {
                level = 100,
            };
            _initialState = _initialState
                .SetState(_signerAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(
                    _avatarAddress.Derive(LegacyInventoryKey),
                    avatarState.inventory.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize())
                .SetState(
                    _avatarAddress.Derive(LegacyQuestListKey),
                    avatarState.questList.Serialize())
                .SetState(GoldCurrencyState.Address, _goldCurrencyState.Serialize());
        }

        public static IEnumerable<object[]> StakeAndClaimStakeRewardTestCases()
        {
            // 일반적인 보상수령 확인
            // 1단계 수준(10~99NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 10 NCG, 1 ap portion / 200 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                10,
                new[]
                {
                    (400000, 1),
                    (500000, 0),
                },
                50400,
            };
            yield return new object[]
            {
                99,
                new[]
                {
                    (400000, 9),
                    (500000, 0),
                },
                50400,
            };

            // 2단계 수준(100~999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 8 NCG, 1 ap portion / 200 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                100,
                new[]
                {
                    (400000, 12),
                    (500000, 0),
                },
                50400,
            };
            yield return new object[]
            {
                200,
                new[]
                {
                    (400000, 25),
                    (500000, 1),
                },
                50400,
            };
            yield return new object[]
            {
                999,
                new[]
                {
                    (400000, 124),
                    (500000, 4),
                },
                50400,
            };

            // 3단계 수준(1,000~9,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 180 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                1000,
                new[]
                {
                    (400000, 200),
                    (500000, 5),
                },
                50400,
            };
            yield return new object[]
            {
                9999,
                new[]
                {
                    (400000, 1999),
                    (500000, 55),
                },
                50400,
            };

            // 4단계 수준(10,000~99,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 180 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                10000,
                new[]
                {
                    (400000, 2000),
                    (500000, 55),
                },
                50400,
            };
            yield return new object[]
            {
                99999,
                new[]
                {
                    (400000, 19999),
                    (500000, 555),
                },
                50400,
            };

            // 5단계 수준(100,000~100,000,000NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 160 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                100000,
                new[]
                {
                    (400000, 20000),
                    (500000, 625),
                },
                50400,
            };
            yield return new object[]
            {
                999999,
                new[]
                {
                    (400000, 199999),
                    (500000, 6249),
                },
                50400,
            };

            // 지연된 보상수령 확인
            // 1단계 수준(10~99NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 보상을 수령하지 않음
            //      → 2번째 보상 시점 (100,800블록 이후) 도달
            //      → 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 2 hourglass / 10 NCG, 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                10,
                new[]
                {
                    (400000, 2),
                    (500000, 0),
                },
                100800,
            };
            yield return new object[]
            {
                99,
                new[]
                {
                    (400000, 18),
                    (500000, 0),
                },
                100800,
            };

            // 2단계 수준(100~999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 보상을 수령하지 않음
            //      → 2번째 보상 시점 (100,800블록 이후) 도달
            //      → 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 2 hourglass / 8 NCG, 2 ap portion / 200 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                100,
                new[]
                {
                    (400000, 24),
                    (500000, 0),
                },
                100800,
            };
            yield return new object[]
            {
                200,
                new[]
                {
                    (400000, 50),
                    (500000, 2),
                },
                100800,
            };
            yield return new object[]
            {
                999,
                new[]
                {
                    (400000, 248),
                    (500000, 8),
                },
                100800,
            };

            // 3단계 수준(1,000~9,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 보상을 수령하지 않음
            //      → 2번째 보상 시점 (100,800블록 이후) 도달
            //      → 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 2 hourglass / 5 NCG, 2 ap portion / 180 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                1000,
                new[]
                {
                    (400000, 400),
                    (500000, 10),
                },
                100800,
            };
            yield return new object[]
            {
                9999,
                new[]
                {
                    (400000, 3998),
                    (500000, 110),
                },
                100800,
            };

            // 4단계 수준(10,000~99,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 보상을 수령하지 않음
            //      → 2번째 보상 시점 (100,800블록 이후) 도달
            //      → 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 2 hourglass / 5 NCG, 2 ap portion / 180 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                10000,
                new[]
                {
                    (400000, 4000),
                    (500000, 110),
                },
                100800,
            };
            yield return new object[]
            {
                99999,
                new[]
                {
                    (400000, 39998),
                    (500000, 1110),
                },
                100800,
            };

            // 5단계 수준(100,000~100,000,000NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 160 NCG 소수점 버림, 기존 deposit 유지 확인)
            // 5단계 수준(100,000~100,000,000NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 보상을 수령하지 않음
            //      → 2번째 보상 시점 (100,800블록 이후) 도달
            //      → 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 2 hourglass / 5 NCG, 2 ap portion / 160 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                100000,
                new[]
                {
                    (400000, 40000),
                    (500000, 1250),
                },
                100800,
            };
            yield return new object[]
            {
                999999,
                new[]
                {
                    (400000, 399998),
                    (500000, 12498),
                },
                100800,
            };
        }

        public static IEnumerable<object[]> StakeLessAfterLockupTestcases()
        {
            (long ClaimBlockIndex, (int ItemId, int Amount)[])[] BuildEvents(
                int hourglassRate,
                int apPotionRate,
                long stakeAmount)
            {
                const int hourglassItemId = 400000, apPotionItemId = 500000;
                return new[]
                {
                    (StakeState.RewardInterval, new[]
                    {
                        (hourglassItemId, (int)(stakeAmount / hourglassRate)),
                        (apPotionItemId, (int)(stakeAmount / apPotionRate)),
                    }),
                    (StakeState.RewardInterval * 2, new[]
                    {
                        (hourglassItemId, (int)(stakeAmount / hourglassRate)),
                        (apPotionItemId, (int)(stakeAmount / apPotionRate)),
                    }),
                    (StakeState.RewardInterval * 3, new[]
                    {
                        (hourglassItemId, (int)(stakeAmount / hourglassRate)),
                        (apPotionItemId, (int)(stakeAmount / apPotionRate)),
                    }),
                    (StakeState.RewardInterval * 4, new[]
                    {
                        (hourglassItemId, (int)(stakeAmount / hourglassRate)),
                        (apPotionItemId, (int)(stakeAmount / apPotionRate)),
                    }),
                };
            }

            // 1단계 수준(10~99NCG)의 deposit save 완료
            //      → 1~3회까지 모든 보상을 수령함
            //      → 201,600 블록 도달 이후
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            //        (보상내용: 1 hourglass / 10 NCG, 1 ap portion / 200 NCG 소수점 버림, 기존 deposit 유지 확인)
            //      → 기존 deposit보다 낮은 금액으로 edit save 한다.
            //      → 보상 타이머 리셋 확인
            //      → (기존 deposit - 현재 deposit 만큼의 ncg 인출 상태 확인)
            //      → 현재 스테이킹된 NCG를 인출할 수 없다 (스테이킹 추가는 가능)
            //      → (현재 deposit 묶인 상태 확인)
            yield return new object[]
            {
                10,
                10,
                9,
                BuildEvents(10, 200, 10),
            };
            yield return new object[]
            {
                99,
                99,
                10,
                BuildEvents(10, 200, 99),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                10,
                10,
                0,
                BuildEvents(10, 200, 10),
            };
            yield return new object[]
            {
                99,
                99,
                0,
                BuildEvents(10, 200, 99),
            };

            // 2단계 수준(100~999NCG)의 deposit save 완료
            //      → 1~3회까지 모든 보상을 수령함
            //      → 201,600 블록 도달 이후
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            //        (보상내용: 1 hourglass / 8 NCG, 1 ap portion / 200 NCG 소수점 버림, 기존 deposit 유지 확인)
            //      → 기존 deposit보다 낮은 금액으로 edit save 한다.
            //      → 보상 타이머 리셋 확인
            //      → (기존 deposit - 현재 deposit 만큼의 ncg 인출 상태 확인)
            //      → 현재 스테이킹된 NCG를 인출할 수 없다 (스테이킹 추가는 가능)
            //      → (현재 deposit 묶인 상태 확인)
            yield return new object[]
            {
                100,
                100,
                99,
                BuildEvents(8, 200, 100),
            };
            yield return new object[]
            {
                999,
                999,
                100,
                BuildEvents(8, 200, 999),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                100,
                100,
                0,
                BuildEvents(8, 200, 100),
            };
            yield return new object[]
            {
                999,
                999,
                0,
                BuildEvents(8, 200, 999),
            };

            // 3단계 수준(1,000~9,999NCG)의 deposit save 완료
            //      → 1~3회까지 모든 보상을 수령함
            //      → 201,600 블록 도달 이후
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            //        (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 180 NCG 소수점 버림, 기존 deposit 유지 확인)
            //      → 기존 deposit보다 낮은 금액으로 edit save 한다.
            //      → 보상 타이머 리셋 확인
            //      → (기존 deposit - 현재 deposit 만큼의 ncg 인출 상태 확인)
            //      → 현재 스테이킹된 NCG를 인출할 수 없다 (스테이킹 추가는 가능)
            //      → (현재 deposit 묶인 상태 확인)
            yield return new object[]
            {
                1000,
                1000,
                999,
                BuildEvents(5, 180, 1000),
            };
            yield return new object[]
            {
                9999,
                9999,
                1000,
                BuildEvents(5, 180, 9999),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                1000,
                1000,
                0,
                BuildEvents(5, 180, 1000),
            };
            yield return new object[]
            {
                9999,
                9999,
                0,
                BuildEvents(5, 180, 9999),
            };

            // 4단계 수준(10,000~99,999NCG)의 deposit save 완료
            //      → 1~3회까지 모든 보상을 수령함
            //      → 201,600 블록 도달 이후
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            //        (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 180 NCG 소수점 버림, 기존 deposit 유지 확인)
            //      → 기존 deposit보다 낮은 금액으로 edit save 한다.
            //      → 보상 타이머 리셋 확인
            //      → (기존 deposit - 현재 deposit 만큼의 ncg 인출 상태 확인)
            //      → 현재 스테이킹된 NCG를 인출할 수 없다 (스테이킹 추가는 가능)
            //      → (현재 deposit 묶인 상태 확인)
            yield return new object[]
            {
                10000,
                10000,
                9999,
                BuildEvents(5, 180, 10000),
            };
            yield return new object[]
            {
                99999,
                99999,
                10000,
                BuildEvents(5, 180, 99999),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                10000,
                10000,
                0,
                BuildEvents(5, 180, 10000),
            };
            yield return new object[]
            {
                99999,
                99999,
                0,
                BuildEvents(5, 180, 99999),
            };

            // 5단계 수준(100,000~100,000,000NCG)의 deposit save 완료
            //      → 1~3회까지 모든 보상을 수령함
            //      → 201,600 블록 도달 이후
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            //        (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 160 NCG 소수점 버림, 기존 deposit 유지 확인)
            //      → 기존 deposit보다 낮은 금액으로 edit save 한다.
            //      → 보상 타이머 리셋 확인
            //      → (기존 deposit - 현재 deposit 만큼의 ncg 인출 상태 확인)
            //      → 현재 스테이킹된 NCG를 인출할 수 없다 (스테이킹 추가는 가능)
            //      → (현재 deposit 묶인 상태 확인)
            yield return new object[]
            {
                100000,
                100000,
                99999,
                BuildEvents(5, 160, 100000),
            };
            yield return new object[]
            {
                100000000,
                100000000,
                100000,
                BuildEvents(5, 160, 100000000),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                100000,
                100000,
                0,
                BuildEvents(5, 160, 100000),
            };
            yield return new object[]
            {
                100000000,
                100000000,
                0,
                BuildEvents(5, 160, 100000000),
            };
        }

        [Theory]
        [MemberData(nameof(StakeAndClaimStakeRewardTestCases))]
        public void StakeAndClaimStakeReward(long stakeAmount, (int ItemId, int Amount)[] expectedItems, long receiveBlockIndex)
        {
            var states = _initialState.MintAsset(_signerAddress, _currency * stakeAmount);

            IAction action = new Stake(stakeAmount);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            Assert.True(states.TryGetStakeState(_signerAddress, out StakeState stakeState));
            Assert.NotNull(stakeState);

            action = new ClaimStakeReward(_avatarAddress);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = receiveBlockIndex,
            });

            // 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            var avatarState = states.GetAvatarStateV2(_avatarAddress);
            foreach ((int itemId, int amount) in expectedItems)
            {
                Assert.True(avatarState.inventory.HasItem(itemId, amount));
            }

            // 기존 deposit 유지 확인
            Assert.Equal(
                _currency * stakeAmount,
                states.GetBalance(stakeState.address, _currency));
        }

        [Theory]
        [InlineData(100, 10, 99, StakeState.LockupInterval - 10)]
        [InlineData(100, 99, 100, StakeState.LockupInterval - 1000)]
        [InlineData(1000, 100, 999, StakeState.LockupInterval - 10000)]
        [InlineData(1000, 999, 1000, StakeState.LockupInterval - 10000)]
        [InlineData(10000, 1000, 9999, StakeState.LockupInterval - 10000)]
        [InlineData(10000, 9999, 10000, StakeState.LockupInterval - 10000)]
        [InlineData(100000, 10000, 99999, StakeState.LockupInterval - 10000)]
        [InlineData(100000, 99999, 100000, StakeState.LockupInterval - 10000)]
        [InlineData(100000000, 100000, 100000000, StakeState.LockupInterval - 10000)]
        public void StakeAndStakeMore(long initialBalance, long stakeAmount, long newStakeAmount, long newStakeBlockIndex)
        {
            // Validate testcases
            Assert.True(newStakeBlockIndex < StakeState.LockupInterval);
            Assert.True(stakeAmount < newStakeAmount);

            var states = _initialState.MintAsset(_signerAddress, _currency * initialBalance);

            IAction action = new Stake(stakeAmount);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            Assert.True(states.TryGetStakeState(_signerAddress, out StakeState stakeState));
            Assert.NotNull(stakeState);
            Assert.Equal(
                _currency * (initialBalance - stakeAmount),
                states.GetBalance(_signerAddress, _currency));
            Assert.Equal(
                _currency * stakeAmount,
                states.GetBalance(stakeState.address, _currency));

            action = new Stake(newStakeAmount);
            // 스테이킹 추가는 가능
            // 락업기간 이전에 deposit을 추가해서 save 할 수 있는지
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = newStakeBlockIndex,
            });

            Assert.True(states.TryGetStakeState(_signerAddress, out stakeState));
            Assert.NotNull(stakeState);
            // 쌓여있던 보상 타이머가 정상적으로 리셋되는지
            Assert.Equal(newStakeBlockIndex, stakeState.StartedBlockIndex);
            // 락업기간이 새롭게 201,600으로 갱신되는지 확인
            Assert.Equal(
                newStakeBlockIndex + StakeState.LockupInterval,
                stakeState.CancellableBlockIndex);
            Assert.Equal(
                _currency * (initialBalance - newStakeAmount),
                states.GetBalance(_signerAddress, _currency));
            // 기존보다 초과해서 설정한 deposit 으로 묶인 상태 갱신된 것 확인
            Assert.Equal(
                _currency * newStakeAmount,
                states.GetBalance(stakeState.address, _currency));
        }

        [Theory]
        [InlineData(100, 10, 1)]
        [InlineData(100, 99, 10)]
        [InlineData(1000, 100, 99)]
        [InlineData(1000, 100, 10)]
        [InlineData(1000, 999, 100)]
        [InlineData(1000, 999, 500)]
        [InlineData(10000, 1000, 999)]
        [InlineData(10000, 9999, 1000)]
        [InlineData(10000, 9999, 5000)]
        [InlineData(100000, 10000, 9999)]
        [InlineData(100000, 99999, 10000)]
        [InlineData(100000, 99999, 50000)]
        [InlineData(100000000, 100000, 9999)]
        [InlineData(100000000, 100000000, 0)]
        [InlineData(100000000, 100000000, 99999999)]
        public void StakeAndStakeLess(long initialBalance, long stakeAmount, long newStakeAmount)
        {
            // Validate testcases
            Assert.True(initialBalance >= stakeAmount);
            Assert.True(newStakeAmount < stakeAmount);

            var states = _initialState.MintAsset(_signerAddress, _currency * initialBalance);

            IAction action = new Stake(stakeAmount);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            Assert.True(states.TryGetStakeState(_signerAddress, out StakeState stakeState));
            Assert.NotNull(stakeState);
            Assert.Equal(
                _currency * (initialBalance - stakeAmount),
                states.GetBalance(_signerAddress, _currency));
            Assert.Equal(
                _currency * stakeAmount,
                states.GetBalance(stakeState.address, _currency));

            action = new Stake(newStakeAmount);
            // 락업기간 이전에 deposit을 감소해서 save할때 락업되어 거부되는가
            Assert.Throws<RequiredBlockIndexException>(() => states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = StakeState.LockupInterval - 1,
            }));

            Assert.True(states.TryGetStakeState(_signerAddress, out stakeState));
            Assert.NotNull(stakeState);
            Assert.Equal(
                _currency * (initialBalance - stakeAmount),
                states.GetBalance(_signerAddress, _currency));
            Assert.Equal(
                _currency * stakeAmount,
                states.GetBalance(stakeState.address, _currency));
        }

        [Theory]
        [MemberData(nameof(StakeLessAfterLockupTestcases))]
        // 락업기간 종료 이후 deposit을 현재보다 낮게 설정했을때, 설정이 잘되서 새롭게 락업되는지 확인
        // 락업기간 종료 이후 보상 수령하고 락업해제되는지 확인
        public void StakeLessAfterLockup(long initialBalance, long stakeAmount, long newStakeAmount, (long ClaimBlockIndex, (int ItemId, int Amount)[] ExpectedItems)[] claimEvents)
        {
            StakeState stakeState;

            // Validate testcases
            Assert.True(stakeAmount > newStakeAmount);

            var states = _initialState.MintAsset(_signerAddress, _currency * initialBalance);

            IAction action = new Stake(stakeAmount);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            // 1~3회까지 모든 보상을 수령함
            // 201,600 블록 도달 이후 → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            foreach ((long claimBlockIndex, (int itemId, int amount)[] expectedItems) in claimEvents)
            {
                action = new ClaimStakeReward(_avatarAddress);
                states = action.Execute(new ActionContext
                {
                    PreviousStates = states,
                    Signer = _signerAddress,
                    BlockIndex = claimBlockIndex,
                });

                var avatarState = states.GetAvatarStateV2(_avatarAddress);
                foreach ((int itemId, int amount) in expectedItems)
                {
                    Assert.True(avatarState.inventory.HasItem(itemId, amount));
                }

                Assert.True(states.TryGetStakeState(_signerAddress, out stakeState));
                Assert.NotNull(stakeState);
                // deposit 유지 확인
                Assert.Equal(
                    _currency * stakeAmount,
                    states.GetBalance(stakeState.address, _currency));
            }

            action = new Stake(newStakeAmount);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = StakeState.LockupInterval,
            });

            // Setup staking again.
            if (newStakeAmount > 0)
            {
                Assert.True(states.TryGetStakeState(_signerAddress, out stakeState));
                Assert.NotNull(stakeState);
                // 쌓여있던 보상 타이머가 정상적으로 리셋되는지
                Assert.Equal(StakeState.LockupInterval, stakeState.StartedBlockIndex);
                // 락업기간이 새롭게 201,600으로 갱신되는지 확인
                Assert.Equal(
                    StakeState.LockupInterval + StakeState.LockupInterval,
                    stakeState.CancellableBlockIndex);
                // 기존 deposit - 현재 deposit 만큼의 ncg 인출 상태 확인
                Assert.Equal(
                    _currency * (initialBalance - newStakeAmount),
                    states.GetBalance(_signerAddress, _currency));
                Assert.Equal(
                    _currency * newStakeAmount,
                    states.GetBalance(stakeState.address, _currency));

                Assert.Throws<RequiredBlockIndexException>(() =>
                {
                    // 현재 스테이킹된 NCG를 인출할 수 없다
                    action = new ClaimStakeReward(_avatarAddress);
                    states = action.Execute(new ActionContext
                    {
                        PreviousStates = states,
                        Signer = _signerAddress,
                        BlockIndex = StakeState.LockupInterval + 1,
                    });
                });
                // 현재 deposit 묶인 상태 확인
                Assert.Equal(
                    _currency * newStakeAmount,
                    states.GetBalance(stakeState.address, _currency));
            }
            else
            {
                Assert.Equal(
                    _currency * initialBalance,
                    states.GetBalance(_signerAddress, _currency));
                Assert.False(states.TryGetStakeState(_signerAddress, out stakeState));
                Assert.Null(stakeState);
            }
        }

        [Fact]
        public void StakeAndClaimStakeRewardBeforeRewardInterval()
        {
            var states = _initialState.MintAsset(_signerAddress, _currency * 100);
            IAction action = new Stake(100);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = 0,
            });

            action = new ClaimStakeReward(_avatarAddress);
            Assert.Throws<RequiredBlockIndexException>(() => states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = StakeState.RewardInterval - 1,
            }));

            var avatarState = states.GetAvatarStateV2(_avatarAddress);
            Assert.Empty(avatarState.inventory.Items.Where(x => x.item.Id == 400000));
            Assert.Empty(avatarState.inventory.Items.Where(x => x.item.Id == 500000));
        }
    }
}
