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
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;
    using State = Lib9c.Tests.Action.State;

    public class StakeAndClaimStakeReward3ScenarioTest
    {
        private readonly IAccountStateDelta _initialState;
        private readonly Currency _currency;
        private readonly GoldCurrencyState _goldCurrencyState;
        private readonly TableSheets _tableSheets;
        private readonly Address _signerAddress;
        private readonly Address _avatarAddress;

        public StakeAndClaimStakeReward3ScenarioTest(ITestOutputHelper outputHelper)
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

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
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
            // 1단계 수준(50~499NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 10 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                50,
                new[]
                {
                    (400000, 5),
                    (500000, 1),
                },
                ClaimStakeReward.ObsoletedIndex + 50400,
                0,
            };
            yield return new object[]
            {
                499,
                new[]
                {
                    (400000, 49),
                    (500000, 1),
                },
                ClaimStakeReward.ObsoletedIndex + 50400,
                0,
            };

            // 2단계 수준(500~4,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 8 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                500,
                new[]
                {
                    (400000, 62),
                    (500000, 2),
                },
                ClaimStakeReward.ObsoletedIndex + 50400,
                0,
            };
            yield return new object[]
            {
                799,
                new[]
                {
                    (400000, 99),
                    (500000, 2),
                },
                ClaimStakeReward.ObsoletedIndex + 50400,
                0,
            };
            yield return new object[]
            {
                4999,
                new[]
                {
                    (400000, 624),
                    (500000, 8),
                },
                ClaimStakeReward.ObsoletedIndex + 50400,
                0,
            };

            // 3단계 수준(5,000~49,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                5000,
                new[]
                {
                    (400000, 1000),
                    (500000, 8),
                },
                ClaimStakeReward.ObsoletedIndex + 50400,
                0,
            };
            yield return new object[]
            {
                49999,
                new[]
                {
                    (400000, 9999),
                    (500000, 64),
                },
                ClaimStakeReward.ObsoletedIndex + 50400,
                8,
            };

            // 4단계 수준(50,000~499,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                50000,
                new[]
                {
                    (400000, 10000),
                    (500000, 64),
                },
                ClaimStakeReward.ObsoletedIndex + 50400,
                8,
            };
            yield return new object[]
            {
                499999,
                new[]
                {
                    (400000, 99999),
                    (500000, 626),
                },
                ClaimStakeReward.ObsoletedIndex + 50400,
                83,
            };

            // 5단계 수준(500,000~100,000,000NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                500000,
                new[]
                {
                    (400000, 100000),
                    (500000, 627),
                },
                ClaimStakeReward.ObsoletedIndex + 50400,
                83,
            };
            yield return new object[]
            {
                99999999,
                new[]
                {
                    (400000, 19999999),
                    (500000, 125001),
                },
                ClaimStakeReward.ObsoletedIndex + 50400,
                16666,
            };

            // 지연된 보상수령 확인
            // 1단계 수준(50~499NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 보상을 수령하지 않음
            //      → 2번째 보상 시점 (100,800블록 이후) 도달
            //      → 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 2 hourglass / 50 NCG, 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                50,
                new[]
                {
                    (400000, 45),
                    (500000, 9),
                },
                ClaimStakeReward.ObsoletedIndex + 500800,
                0,
            };
            yield return new object[]
            {
                499,
                new[]
                {
                    (400000, 441),
                    (500000, 9),
                },
                ClaimStakeReward.ObsoletedIndex + 500800,
                0,
            };

            // 2단계 수준(500~4,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 보상을 수령하지 않음
            //      → 2번째 보상 시점 (100,800블록 이후) 도달
            //      → 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 2 hourglass / 8 NCG, 2 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                500,
                new[]
                {
                    (400000, 558),
                    (500000, 18),
                },
                ClaimStakeReward.ObsoletedIndex + 500800,
                0,
            };
            yield return new object[]
            {
                799,
                new[]
                {
                    (400000, 891),
                    (500000, 18),
                },
                ClaimStakeReward.ObsoletedIndex + 500800,
                0,
            };
            yield return new object[]
            {
                4999,
                new[]
                {
                    (400000, 5616),
                    (500000, 72),
                },
                ClaimStakeReward.ObsoletedIndex + 500800,
                0,
            };

            // 3단계 수준(5,000~49,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 보상을 수령하지 않음
            //      → 2번째 보상 시점 (100,800블록 이후) 도달
            //      → 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 2 hourglass / 5 NCG, 2 ap portion / 180 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                5000,
                new[]
                {
                    (400000, 9000),
                    (500000, 72),
                },
                ClaimStakeReward.ObsoletedIndex + 500800,
                0,
            };
            yield return new object[]
            {
                49999,
                new[]
                {
                    (400000, 89991),
                    (500000, 576),
                },
                ClaimStakeReward.ObsoletedIndex + 500800,
                72,
            };

            // 4단계 수준(50,000~499,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 보상을 수령하지 않음
            //      → 2번째 보상 시점 (100,800블록 이후) 도달
            //      → 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 2 hourglass / 5 NCG, 2 ap portion / 180 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                50000,
                new[]
                {
                    (400000, 90000),
                    (500000, 576),
                },
                ClaimStakeReward.ObsoletedIndex + 500800,
                72,
            };
            yield return new object[]
            {
                499999,
                new[]
                {
                    (400000, 899991),
                    (500000, 5634),
                },
                ClaimStakeReward.ObsoletedIndex + 500800,
                747,
            };

            // 5단계 수준(500,000~4,999,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 160 NCG 소수점 버림, 기존 deposit 유지 확인)
            // 5단계 수준(500,000~500,000,000NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 보상을 수령하지 않음
            //      → 2번째 보상 시점 (100,800블록 이후) 도달
            //      → 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 2 hourglass / 5 NCG, 2 ap portion / 160 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                500000,
                new[]
                {
                    (400000, 900000),
                    (500000, 5643),
                },
                ClaimStakeReward.ObsoletedIndex + 500800,
                747,
            };
            yield return new object[]
            {
                4999999,
                new[]
                {
                    (400000, 8999991),
                    (500000, 56259),
                },
                ClaimStakeReward.ObsoletedIndex + 500800,
                7497,
            };
        }

        public static IEnumerable<object[]> StakeLessAfterLockupTestcases()
        {
            (long ClaimBlockIndex, (int ItemId, int Amount)[])[] BuildEvents(
                int hourglassRate,
                int apPotionRate,
                int apPotionBonus,
                long stakeAmount)
            {
                const int hourglassItemId = 400000, apPotionItemId = 500000;
                return new[]
                {
                    (ClaimStakeReward.ObsoletedIndex + StakeState.RewardInterval, new[]
                    {
                        (hourglassItemId, (int)(stakeAmount / hourglassRate)),
                        (apPotionItemId, (int)(stakeAmount / apPotionRate + apPotionBonus)),
                    }),
                    (ClaimStakeReward.ObsoletedIndex + StakeState.RewardInterval * 2, new[]
                    {
                        (hourglassItemId, (int)(stakeAmount / hourglassRate)),
                        (apPotionItemId, (int)(stakeAmount / apPotionRate + apPotionBonus)),
                    }),
                    (ClaimStakeReward.ObsoletedIndex + StakeState.RewardInterval * 3, new[]
                    {
                        (hourglassItemId, (int)(stakeAmount / hourglassRate)),
                        (apPotionItemId, (int)(stakeAmount / apPotionRate + apPotionBonus)),
                    }),
                    (ClaimStakeReward.ObsoletedIndex + StakeState.RewardInterval * 4, new[]
                    {
                        (hourglassItemId, (int)(stakeAmount / hourglassRate)),
                        (apPotionItemId, (int)(stakeAmount / apPotionRate + apPotionBonus)),
                    }),
                };
            }

            // 1단계 수준(50~499NCG)의 deposit save 완료
            //      → 1~3회까지 모든 보상을 수령함
            //      → 201,600 블록 도달 이후
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            //        (보상내용: 1 hourglass / 50 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            //      → 기존 deposit보다 낮은 금액으로 edit save 한다.
            //      → 보상 타이머 리셋 확인
            //      → (기존 deposit - 현재 deposit 만큼의 ncg 인출 상태 확인)
            //      → 현재 스테이킹된 NCG를 인출할 수 없다 (스테이킹 추가는 가능)
            //      → (현재 deposit 묶인 상태 확인)
            yield return new object[]
            {
                51,
                51,
                50,
                BuildEvents(10, 800, 1, 50),
            };
            yield return new object[]
            {
                499,
                499,
                50,
                BuildEvents(10, 800, 1, 499),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                50,
                50,
                0,
                BuildEvents(10, 800, 1, 50),
            };
            yield return new object[]
            {
                499,
                499,
                0,
                BuildEvents(10, 800, 1, 499),
            };

            // 2단계 수준(500~4,999NCG)의 deposit save 완료
            //      → 1~3회까지 모든 보상을 수령함
            //      → 201,600 블록 도달 이후
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            //        (보상내용: 1 hourglass / 8 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            //      → 기존 deposit보다 낮은 금액으로 edit save 한다.
            //      → 보상 타이머 리셋 확인
            //      → (기존 deposit - 현재 deposit 만큼의 ncg 인출 상태 확인)
            //      → 현재 스테이킹된 NCG를 인출할 수 없다 (스테이킹 추가는 가능)
            //      → (현재 deposit 묶인 상태 확인)
            yield return new object[]
            {
                500,
                500,
                499,
                BuildEvents(8, 800, 2, 500),
            };
            yield return new object[]
            {
                4999,
                4999,
                500,
                BuildEvents(8, 800, 2, 4999),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                500,
                500,
                0,
                BuildEvents(8, 800, 2, 500),
            };
            yield return new object[]
            {
                4999,
                4999,
                0,
                BuildEvents(8, 800, 2, 4999),
            };

            // 3단계 수준(5,000~49,999NCG)의 deposit save 완료
            //      → 1~3회까지 모든 보상을 수령함
            //      → 201,600 블록 도달 이후
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            //        (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            //      → 기존 deposit보다 낮은 금액으로 edit save 한다.
            //      → 보상 타이머 리셋 확인
            //      → (기존 deposit - 현재 deposit 만큼의 ncg 인출 상태 확인)
            //      → 현재 스테이킹된 NCG를 인출할 수 없다 (스테이킹 추가는 가능)
            //      → (현재 deposit 묶인 상태 확인)
            yield return new object[]
            {
                5000,
                5000,
                4999,
                BuildEvents(5, 800, 2, 5000),
            };
            yield return new object[]
            {
                49999,
                49999,
                5000,
                BuildEvents(5, 800, 2, 49999),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                5000,
                5000,
                0,
                BuildEvents(5, 800, 2, 5000),
            };
            yield return new object[]
            {
                49999,
                49999,
                0,
                BuildEvents(5, 800, 2, 49999),
            };

            // 4단계 수준(50,000~499,999NCG)의 deposit save 완료
            //      → 1~3회까지 모든 보상을 수령함
            //      → 201,600 블록 도달 이후
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            //        (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            //      → 기존 deposit보다 낮은 금액으로 edit save 한다.
            //      → 보상 타이머 리셋 확인
            //      → (기존 deposit - 현재 deposit 만큼의 ncg 인출 상태 확인)
            //      → 현재 스테이킹된 NCG를 인출할 수 없다 (스테이킹 추가는 가능)
            //      → (현재 deposit 묶인 상태 확인)
            yield return new object[]
            {
                50000,
                50000,
                49999,
                BuildEvents(5, 800, 2, 50000),
            };
            yield return new object[]
            {
                499999,
                499999,
                50000,
                BuildEvents(5, 800, 2, 499999),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                50000,
                50000,
                0,
                BuildEvents(5, 800, 2, 50000),
            };
            yield return new object[]
            {
                499999,
                499999,
                0,
                BuildEvents(5, 800, 2, 499999),
            };

            // 5단계 수준(500,000~100,000,000NCG)의 deposit save 완료
            //      → 1~3회까지 모든 보상을 수령함
            //      → 201,600 블록 도달 이후
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            //        (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            //      → 기존 deposit보다 낮은 금액으로 edit save 한다.
            //      → 보상 타이머 리셋 확인
            //      → (기존 deposit - 현재 deposit 만큼의 ncg 인출 상태 확인)
            //      → 현재 스테이킹된 NCG를 인출할 수 없다 (스테이킹 추가는 가능)
            //      → (현재 deposit 묶인 상태 확인)
            yield return new object[]
            {
                500000,
                500000,
                499999,
                BuildEvents(5, 800, 2, 500000),
            };
            yield return new object[]
            {
                500000000,
                500000000,
                500000,
                BuildEvents(5, 800, 2, 500000000),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                500000,
                500000,
                0,
                BuildEvents(5, 800, 2, 500000),
            };
            yield return new object[]
            {
                500000000,
                500000000,
                0,
                BuildEvents(5, 800, 2, 500000000),
            };
        }

        [Theory]
        [MemberData(nameof(StakeAndClaimStakeRewardTestCases))]
        public void StakeAndClaimStakeReward(long stakeAmount, (int ItemId, int Amount)[] expectedItems, long receiveBlockIndex, int expectedRune)
        {
            var states = _initialState.MintAsset(_signerAddress, _currency * stakeAmount);

            IAction action = new Stake(stakeAmount);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = ClaimStakeReward.ObsoletedIndex,
            });

            Assert.True(states.TryGetStakeState(_signerAddress, out StakeState stakeState));
            Assert.NotNull(stakeState);
            Assert.Equal(0 * RuneHelper.StakeRune, _initialState.GetBalance(_avatarAddress, RuneHelper.StakeRune));

            action = new ClaimStakeReward3(_avatarAddress);
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
            Assert.Equal(expectedRune * RuneHelper.StakeRune, states.GetBalance(_avatarAddress, RuneHelper.StakeRune));
        }

        [Theory]
        [InlineData(500, 50, 499)]
        [InlineData(500, 499, 500)]
        [InlineData(5000, 500, 4999)]
        [InlineData(5000, 4999, 5000)]
        [InlineData(50000, 5000, 49999)]
        [InlineData(50000, 49999, 50000)]
        [InlineData(500000, 50000, 499999)]
        [InlineData(500000, 499999, 500000)]
        [InlineData(500000000, 500000, 500000000)]
        public void StakeAndStakeMore(long initialBalance, long stakeAmount, long newStakeAmount)
        {
            long newStakeBlockIndex = ClaimStakeReward.ObsoletedIndex + StakeState.LockupInterval - 1;
            // Validate testcases
            Assert.True(stakeAmount < newStakeAmount);

            var states = _initialState.MintAsset(_signerAddress, _currency * initialBalance);

            IAction action = new Stake(stakeAmount);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = ClaimStakeReward.ObsoletedIndex,
            });

            Assert.True(states.TryGetStakeState(_signerAddress, out StakeState stakeState));
            Assert.NotNull(stakeState);
            Assert.Equal(
                _currency * (initialBalance - stakeAmount),
                states.GetBalance(_signerAddress, _currency));
            Assert.Equal(
                _currency * stakeAmount,
                states.GetBalance(stakeState.address, _currency));

            action = new ClaimStakeReward3(_avatarAddress);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = newStakeBlockIndex,
            });

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
        [InlineData(500, 51, 50)]
        [InlineData(500, 499, 50)]
        [InlineData(5000, 500, 499)]
        [InlineData(5000, 500, 50)]
        [InlineData(5000, 4999, 500)]
        [InlineData(50000, 5000, 4999)]
        [InlineData(50000, 49999, 5000)]
        [InlineData(500000, 50000, 49999)]
        [InlineData(500000, 499999, 50000)]
        [InlineData(500000000, 500000, 99999)]
        [InlineData(500000000, 500000000, 0)]
        [InlineData(500000000, 500000000, 99999999)]
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
                BlockIndex = ClaimStakeReward.ObsoletedIndex,
            });

            Assert.True(states.TryGetStakeState(_signerAddress, out StakeState stakeState));
            Assert.NotNull(stakeState);
            Assert.Equal(
                _currency * (initialBalance - stakeAmount),
                states.GetBalance(_signerAddress, _currency));
            Assert.Equal(
                _currency * stakeAmount,
                states.GetBalance(stakeState.address, _currency));

            action = new ClaimStakeReward3(_avatarAddress);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = ClaimStakeReward.ObsoletedIndex + StakeState.LockupInterval - 1,
            });

            action = new Stake(newStakeAmount);
            // 락업기간 이전에 deposit을 감소해서 save할때 락업되어 거부되는가
            Assert.Throws<RequiredBlockIndexException>(() => states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = ClaimStakeReward.ObsoletedIndex + StakeState.LockupInterval - 1,
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
                BlockIndex = ClaimStakeReward.ObsoletedIndex,
            });

            // 1~3회까지 모든 보상을 수령함
            // 201,600 블록 도달 이후 → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            foreach ((long claimBlockIndex, (int itemId, int amount)[] expectedItems) in claimEvents)
            {
                action = new ClaimStakeReward3(_avatarAddress);
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
                BlockIndex = ClaimStakeReward.ObsoletedIndex + StakeState.LockupInterval,
            });

            // Setup staking again.
            if (newStakeAmount > 0)
            {
                Assert.True(states.TryGetStakeState(_signerAddress, out stakeState));
                Assert.NotNull(stakeState);
                // 쌓여있던 보상 타이머가 정상적으로 리셋되는지
                Assert.Equal(ClaimStakeReward.ObsoletedIndex + StakeState.LockupInterval, stakeState.StartedBlockIndex);
                // 락업기간이 새롭게 201,600으로 갱신되는지 확인
                Assert.Equal(
                    ClaimStakeReward.ObsoletedIndex + StakeState.LockupInterval + StakeState.LockupInterval,
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
                    action = new ClaimStakeReward3(_avatarAddress);
                    states = action.Execute(new ActionContext
                    {
                        PreviousStates = states,
                        Signer = _signerAddress,
                        BlockIndex = ClaimStakeReward.ObsoletedIndex + StakeState.LockupInterval + 1,
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
            var states = _initialState.MintAsset(_signerAddress, _currency * 500);
            IAction action = new Stake(500);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = ClaimStakeReward.ObsoletedIndex,
            });

            action = new ClaimStakeReward3(_avatarAddress);
            Assert.Throws<RequiredBlockIndexException>(() => states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signerAddress,
                BlockIndex = ClaimStakeReward.ObsoletedIndex + StakeState.RewardInterval - 1,
            }));

            var avatarState = states.GetAvatarStateV2(_avatarAddress);
            Assert.Empty(avatarState.inventory.Items.Where(x => x.item.Id == 400000));
            Assert.Empty(avatarState.inventory.Items.Where(x => x.item.Id == 500000));
        }
    }
}
