namespace Lib9c.Tests.Action.Scenario
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Util;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class StakeAndClaimStakeReward3ScenarioTest
    {
        private readonly Address _agentAddr;
        private readonly Address _avatarAddr;
        private readonly IAccountStateDelta _initialStatesWithAvatarStateV2;
        private readonly Currency _ncg;

        public StakeAndClaimStakeReward3ScenarioTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            (
                _,
                _agentAddr,
                _avatarAddr,
                _,
                _initialStatesWithAvatarStateV2) = InitializeUtil.InitializeStates();
            _ncg = _initialStatesWithAvatarStateV2.GetGoldCurrency();
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
                50L,
                new[]
                {
                    (400_000, 5),
                    (500_000, 1),
                },
                ClaimStakeReward2.ObsoletedIndex + 50_400L,
                0,
            };
            yield return new object[]
            {
                499L,
                new[]
                {
                    (400_000, 49),
                    (500_000, 1),
                },
                ClaimStakeReward2.ObsoletedIndex + 50_400L,
                0,
            };

            // 2단계 수준(500~4,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 8 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                500L,
                new[]
                {
                    (400_000, 62),
                    (500_000, 2),
                },
                ClaimStakeReward2.ObsoletedIndex + 50_400L,
                0,
            };
            yield return new object[]
            {
                799L,
                new[]
                {
                    (400_000, 99),
                    (500_000, 2),
                },
                ClaimStakeReward2.ObsoletedIndex + 50_400L,
                0,
            };
            yield return new object[]
            {
                4_999L,
                new[]
                {
                    (400_000, 624),
                    (500_000, 8),
                },
                ClaimStakeReward2.ObsoletedIndex + 50_400L,
                0,
            };

            // 3단계 수준(5,000~49,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                5_000L,
                new[]
                {
                    (400_000, 1000),
                    (500_000, 8),
                },
                ClaimStakeReward2.ObsoletedIndex + 50_400L,
                0,
            };
            yield return new object[]
            {
                49_999L,
                new[]
                {
                    (400_000, 9_999),
                    (500_000, 64),
                },
                ClaimStakeReward2.ObsoletedIndex + 50_400L,
                8,
            };

            // 4단계 수준(50,000~499,999NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                50_000L,
                new[]
                {
                    (400_000, 10_000),
                    (500_000, 64),
                },
                ClaimStakeReward2.ObsoletedIndex + 50_400L,
                8,
            };
            yield return new object[]
            {
                499_999L,
                new[]
                {
                    (400_000, 99_999),
                    (500_000, 626),
                },
                ClaimStakeReward2.ObsoletedIndex + 50_400L,
                83,
            };

            // 5단계 수준(500,000~100,000,000NCG)의 deposit save 완료
            //      → 최초 보상 시점 (50,400블록 이후) 도달
            //      → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            // (보상내용: 1 hourglass / 5 NCG, 1 ap portion / 800 NCG 소수점 버림, 기존 deposit 유지 확인)
            yield return new object[]
            {
                500_000L,
                new[]
                {
                    (400_000, 100_000),
                    (500_000, 627),
                },
                ClaimStakeReward2.ObsoletedIndex + 50_400L,
                83,
            };
            yield return new object[]
            {
                99_999_999L,
                new[]
                {
                    (400_000, 19_999_999),
                    (500_000, 125_001),
                },
                ClaimStakeReward2.ObsoletedIndex + 50_400L,
                16_666,
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
                50L,
                new[]
                {
                    (400_000, 45),
                    (500_000, 9),
                },
                ClaimStakeReward2.ObsoletedIndex + 500_800L,
                0,
            };
            yield return new object[]
            {
                499L,
                new[]
                {
                    (400_000, 441),
                    (500_000, 9),
                },
                ClaimStakeReward2.ObsoletedIndex + 500_800L,
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
                500L,
                new[]
                {
                    (400_000, 558),
                    (500_000, 18),
                },
                ClaimStakeReward2.ObsoletedIndex + 500_800L,
                0,
            };
            yield return new object[]
            {
                799L,
                new[]
                {
                    (400_000, 891),
                    (500_000, 18),
                },
                ClaimStakeReward2.ObsoletedIndex + 500_800L,
                0,
            };
            yield return new object[]
            {
                4_999L,
                new[]
                {
                    (400_000, 5_616),
                    (500_000, 72),
                },
                ClaimStakeReward2.ObsoletedIndex + 500_800L,
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
                5_000L,
                new[]
                {
                    (400_000, 9_000),
                    (500_000, 72),
                },
                ClaimStakeReward2.ObsoletedIndex + 500_800L,
                0,
            };
            yield return new object[]
            {
                49_999L,
                new[]
                {
                    (400_000, 89_991),
                    (500_000, 576),
                },
                ClaimStakeReward2.ObsoletedIndex + 500_800L,
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
                50_000L,
                new[]
                {
                    (400_000, 90_000),
                    (500_000, 576),
                },
                ClaimStakeReward2.ObsoletedIndex + 500_800L,
                72,
            };
            yield return new object[]
            {
                499_999L,
                new[]
                {
                    (400_000, 899_991),
                    (500_000, 5_634),
                },
                ClaimStakeReward2.ObsoletedIndex + 500_800L,
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
                500_000L,
                new[]
                {
                    (400_000, 900_000),
                    (500_000, 5_643),
                },
                ClaimStakeReward2.ObsoletedIndex + 500_800L,
                747,
            };
            yield return new object[]
            {
                4_999_999L,
                new[]
                {
                    (400_000, 8_999_991),
                    (500_000, 56_259),
                },
                ClaimStakeReward2.ObsoletedIndex + 500_800L,
                7_497,
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
                const int hourglassItemId = 400_000, apPotionItemId = 500_000;
                return new[]
                {
                    (ClaimStakeReward2.ObsoletedIndex + StakeState.RewardInterval, new[]
                    {
                        (hourglassItemId, (int)(stakeAmount / hourglassRate)),
                        (apPotionItemId, (int)(stakeAmount / apPotionRate + apPotionBonus)),
                    }),
                    (ClaimStakeReward2.ObsoletedIndex + StakeState.RewardInterval * 2, new[]
                    {
                        (hourglassItemId, (int)(stakeAmount / hourglassRate)),
                        (apPotionItemId, (int)(stakeAmount / apPotionRate + apPotionBonus)),
                    }),
                    (ClaimStakeReward2.ObsoletedIndex + StakeState.RewardInterval * 3, new[]
                    {
                        (hourglassItemId, (int)(stakeAmount / hourglassRate)),
                        (apPotionItemId, (int)(stakeAmount / apPotionRate + apPotionBonus)),
                    }),
                    (ClaimStakeReward2.ObsoletedIndex + StakeState.RewardInterval * 4, new[]
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
                51L,
                51L,
                50L,
                BuildEvents(10, 800, 1, 50),
            };
            yield return new object[]
            {
                499L,
                499L,
                50L,
                BuildEvents(10, 800, 1, 499),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                50L,
                50L,
                0L,
                BuildEvents(10, 800, 1, 50),
            };
            yield return new object[]
            {
                499L,
                499L,
                0L,
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
                500L,
                500L,
                499L,
                BuildEvents(8, 800, 2, 500),
            };
            yield return new object[]
            {
                4_999L,
                4_999L,
                500L,
                BuildEvents(8, 800, 2, 4_999),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                500L,
                500L,
                0L,
                BuildEvents(8, 800, 2, 500),
            };
            yield return new object[]
            {
                4_999L,
                4_999L,
                0L,
                BuildEvents(8, 800, 2, 4_999),
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
                5_000L,
                5_000L,
                4_999L,
                BuildEvents(5, 800, 2, 5_000),
            };
            yield return new object[]
            {
                49_999L,
                49_999L,
                5_000L,
                BuildEvents(5, 800, 2, 49_999),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                5_000L,
                5_000L,
                0L,
                BuildEvents(5, 800, 2, 5_000),
            };
            yield return new object[]
            {
                49_999L,
                49_999L,
                0L,
                BuildEvents(5, 800, 2, 49_999),
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
                50_000L,
                50_000L,
                49_999L,
                BuildEvents(5, 800, 2, 50_000),
            };
            yield return new object[]
            {
                499_999L,
                499_999L,
                50_000L,
                BuildEvents(5, 800, 2, 499_999),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                50_000L,
                50_000L,
                0L,
                BuildEvents(5, 800, 2, 50_000),
            };
            yield return new object[]
            {
                499_999L,
                499_999L,
                0L,
                BuildEvents(5, 800, 2, 499_999),
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
                500_000L,
                500_000L,
                499_999L,
                BuildEvents(5, 800, 2, 500_000),
            };
            yield return new object[]
            {
                500_000_000L,
                500_000_000L,
                500_000L,
                BuildEvents(5, 800, 2, 500_000_000),
            };

            // 현재의 스테이킹된 NCG의 전액 인출을 시도한다(deposit NCG 인출 상태 확인)
            //      → 스테이킹 완전 소멸 확인
            yield return new object[]
            {
                500_000L,
                500_000L,
                0L,
                BuildEvents(5, 800, 2, 500_000),
            };
            yield return new object[]
            {
                500_000_000L,
                500_000_000L,
                0L,
                BuildEvents(5, 800, 2, 500_000_000),
            };
        }

        [Theory]
        [MemberData(nameof(StakeAndClaimStakeRewardTestCases))]
        public void StakeAndClaimStakeReward(
            long stakeAmount,
            (int ItemId, int Amount)[] expectedItems,
            long receiveBlockIndex,
            int expectedRune)
        {
            var context = new ActionContext();
            var states = _initialStatesWithAvatarStateV2.MintAsset(context, _agentAddr, _ncg * stakeAmount);

            IAction action = new Stake(stakeAmount);
            states = action.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _agentAddr,
                BlockIndex = ClaimStakeReward2.ObsoletedIndex,
            });

            Assert.True(states.TryGetStakeState(_agentAddr, out StakeState stakeState));
            Assert.NotNull(stakeState);
            Assert.Equal(0 * RuneHelper.StakeRune, _initialStatesWithAvatarStateV2.GetBalance(_avatarAddr, RuneHelper.StakeRune));

            action = new ClaimStakeReward3(_avatarAddr);
            states = action.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _agentAddr,
                BlockIndex = receiveBlockIndex,
            });

            // 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            var avatarState = states.GetAvatarStateV2(_avatarAddr);
            foreach ((int itemId, int amount) in expectedItems)
            {
                Assert.True(avatarState.inventory.HasItem(itemId, amount));
            }

            // 기존 deposit 유지 확인
            Assert.Equal(
                _ncg * stakeAmount,
                states.GetBalance(stakeState.address, _ncg));
            Assert.Equal(expectedRune * RuneHelper.StakeRune, states.GetBalance(_avatarAddr, RuneHelper.StakeRune));
        }

        [Theory]
        [InlineData(500L, 50L, 499L)]
        [InlineData(500L, 499L, 500L)]
        [InlineData(5_000L, 500L, 4_999L)]
        [InlineData(5_000L, 4_999L, 5_000L)]
        [InlineData(50_000L, 5_000L, 49_999L)]
        [InlineData(50_000L, 49_999L, 50_000L)]
        [InlineData(500_000L, 50_000L, 499_999L)]
        [InlineData(500_000L, 499_999L, 500_000L)]
        [InlineData(500_000_000L, 500_000L, 500_000_000L)]
        public void StakeAndStakeMore(long initialBalance, long stakeAmount, long newStakeAmount)
        {
            long newStakeBlockIndex = ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval - 1;
            // Validate testcases
            Assert.True(stakeAmount < newStakeAmount);

            var context = new ActionContext();
            var states = _initialStatesWithAvatarStateV2.MintAsset(context, _agentAddr, _ncg * initialBalance);

            IAction action = new Stake(stakeAmount);
            states = action.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _agentAddr,
                BlockIndex = ClaimStakeReward2.ObsoletedIndex,
            });

            Assert.True(states.TryGetStakeState(_agentAddr, out StakeState stakeState));
            Assert.NotNull(stakeState);
            Assert.Equal(
                _ncg * (initialBalance - stakeAmount),
                states.GetBalance(_agentAddr, _ncg));
            Assert.Equal(
                _ncg * stakeAmount,
                states.GetBalance(stakeState.address, _ncg));

            action = new ClaimStakeReward3(_avatarAddr);
            states = action.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _agentAddr,
                BlockIndex = newStakeBlockIndex,
            });

            action = new Stake(newStakeAmount);
            // 스테이킹 추가는 가능
            // 락업기간 이전에 deposit을 추가해서 save 할 수 있는지
            states = action.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _agentAddr,
                BlockIndex = newStakeBlockIndex,
            });

            Assert.True(states.TryGetStakeState(_agentAddr, out stakeState));
            Assert.NotNull(stakeState);
            // 쌓여있던 보상 타이머가 정상적으로 리셋되는지
            Assert.Equal(newStakeBlockIndex, stakeState.StartedBlockIndex);
            // 락업기간이 새롭게 201,600으로 갱신되는지 확인
            Assert.Equal(
                newStakeBlockIndex + StakeState.LockupInterval,
                stakeState.CancellableBlockIndex);
            Assert.Equal(
                _ncg * (initialBalance - newStakeAmount),
                states.GetBalance(_agentAddr, _ncg));
            // 기존보다 초과해서 설정한 deposit 으로 묶인 상태 갱신된 것 확인
            Assert.Equal(
                _ncg * newStakeAmount,
                states.GetBalance(stakeState.address, _ncg));
        }

        [Theory]
        [InlineData(500L, 51L, 50L)]
        [InlineData(500L, 499L, 50L)]
        [InlineData(5_000L, 500L, 499L)]
        [InlineData(5_000L, 500L, 50L)]
        [InlineData(5_000L, 4_999L, 500L)]
        [InlineData(50_000L, 5_000L, 4_999L)]
        [InlineData(50_000L, 49_999L, 5_000L)]
        [InlineData(500_000L, 50_000L, 49_999L)]
        [InlineData(500_000L, 499_999L, 50_000L)]
        [InlineData(500_000_000L, 500_000L, 99_999L)]
        [InlineData(500_000_000L, 500_000_000L, 0L)]
        [InlineData(500_000_000L, 500_000_000L, 99_999_999L)]
        public void StakeAndStakeLess(long initialBalance, long stakeAmount, long newStakeAmount)
        {
            // Validate testcases
            Assert.True(initialBalance >= stakeAmount);
            Assert.True(newStakeAmount < stakeAmount);

            var context = new ActionContext();
            var states = _initialStatesWithAvatarStateV2.MintAsset(context, _agentAddr, _ncg * initialBalance);

            IAction action = new Stake(stakeAmount);
            states = action.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _agentAddr,
                BlockIndex = ClaimStakeReward2.ObsoletedIndex,
            });

            Assert.True(states.TryGetStakeState(_agentAddr, out StakeState stakeState));
            Assert.NotNull(stakeState);
            Assert.Equal(
                _ncg * (initialBalance - stakeAmount),
                states.GetBalance(_agentAddr, _ncg));
            Assert.Equal(
                _ncg * stakeAmount,
                states.GetBalance(stakeState.address, _ncg));

            action = new ClaimStakeReward3(_avatarAddr);
            states = action.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _agentAddr,
                BlockIndex = ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval - 1,
            });

            action = new Stake(newStakeAmount);
            // 락업기간 이전에 deposit을 감소해서 save할때 락업되어 거부되는가
            Assert.Throws<RequiredBlockIndexException>(() => states = action.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _agentAddr,
                BlockIndex = ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval - 1,
            }));

            Assert.True(states.TryGetStakeState(_agentAddr, out stakeState));
            Assert.NotNull(stakeState);
            Assert.Equal(
                _ncg * (initialBalance - stakeAmount),
                states.GetBalance(_agentAddr, _ncg));
            Assert.Equal(
                _ncg * stakeAmount,
                states.GetBalance(stakeState.address, _ncg));
        }

        [Theory]
        [MemberData(nameof(StakeLessAfterLockupTestcases))]
        // 락업기간 종료 이후 deposit을 현재보다 낮게 설정했을때, 설정이 잘되서 새롭게 락업되는지 확인
        // 락업기간 종료 이후 보상 수령하고 락업해제되는지 확인
        public void StakeLessAfterLockup(
            long initialBalance,
            long stakeAmount,
            long newStakeAmount,
            (long ClaimBlockIndex, (int ItemId, int Amount)[] ExpectedItems)[] claimEvents)
        {
            StakeState stakeState;

            // Validate testcases
            Assert.True(stakeAmount > newStakeAmount);

            var context = new ActionContext();
            var states = _initialStatesWithAvatarStateV2.MintAsset(context, _agentAddr, _ncg * initialBalance);

            IAction action = new Stake(stakeAmount);
            states = action.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _agentAddr,
                BlockIndex = ClaimStakeReward2.ObsoletedIndex,
            });

            // 1~3회까지 모든 보상을 수령함
            // 201,600 블록 도달 이후 → 지정된 캐릭터 앞으로 이하 보상의 수령이 가능해야 한다.
            foreach ((long claimBlockIndex, (int itemId, int amount)[] expectedItems) in claimEvents)
            {
                action = new ClaimStakeReward3(_avatarAddr);
                states = action.Execute(new ActionContext
                {
                    PreviousState = states,
                    Signer = _agentAddr,
                    BlockIndex = claimBlockIndex,
                });

                var avatarState = states.GetAvatarStateV2(_avatarAddr);
                foreach ((int itemId, int amount) in expectedItems)
                {
                    Assert.True(avatarState.inventory.HasItem(itemId, amount));
                }

                Assert.True(states.TryGetStakeState(_agentAddr, out stakeState));
                Assert.NotNull(stakeState);
                // deposit 유지 확인
                Assert.Equal(
                    _ncg * stakeAmount,
                    states.GetBalance(stakeState.address, _ncg));
            }

            action = new Stake(newStakeAmount);
            states = action.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _agentAddr,
                BlockIndex = ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval,
            });

            // Setup staking again.
            if (newStakeAmount > 0)
            {
                Assert.True(states.TryGetStakeState(_agentAddr, out stakeState));
                Assert.NotNull(stakeState);
                // 쌓여있던 보상 타이머가 정상적으로 리셋되는지
                Assert.Equal(ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval, stakeState.StartedBlockIndex);
                // 락업기간이 새롭게 201,600으로 갱신되는지 확인
                Assert.Equal(
                    ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval + StakeState.LockupInterval,
                    stakeState.CancellableBlockIndex);
                // 기존 deposit - 현재 deposit 만큼의 ncg 인출 상태 확인
                Assert.Equal(
                    _ncg * (initialBalance - newStakeAmount),
                    states.GetBalance(_agentAddr, _ncg));
                Assert.Equal(
                    _ncg * newStakeAmount,
                    states.GetBalance(stakeState.address, _ncg));

                Assert.Throws<RequiredBlockIndexException>(() =>
                {
                    // 현재 스테이킹된 NCG를 인출할 수 없다
                    action = new ClaimStakeReward3(_avatarAddr);
                    states = action.Execute(new ActionContext
                    {
                        PreviousState = states,
                        Signer = _agentAddr,
                        BlockIndex = ClaimStakeReward2.ObsoletedIndex + StakeState.LockupInterval + 1,
                    });
                });
                // 현재 deposit 묶인 상태 확인
                Assert.Equal(
                    _ncg * newStakeAmount,
                    states.GetBalance(stakeState.address, _ncg));
            }
            else
            {
                Assert.Equal(
                    _ncg * initialBalance,
                    states.GetBalance(_agentAddr, _ncg));
                Assert.False(states.TryGetStakeState(_agentAddr, out stakeState));
                Assert.Null(stakeState);
            }
        }

        [Fact]
        public void StakeAndClaimStakeRewardBeforeRewardInterval()
        {
            var context = new ActionContext();
            var states = _initialStatesWithAvatarStateV2.MintAsset(context, _agentAddr, _ncg * 500);
            IAction action = new Stake(500);
            states = action.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _agentAddr,
                BlockIndex = ClaimStakeReward2.ObsoletedIndex,
            });

            action = new ClaimStakeReward3(_avatarAddr);
            Assert.Throws<RequiredBlockIndexException>(() => states = action.Execute(new ActionContext
            {
                PreviousState = states,
                Signer = _agentAddr,
                BlockIndex = ClaimStakeReward2.ObsoletedIndex + StakeState.RewardInterval - 1,
            }));

            var avatarState = states.GetAvatarStateV2(_avatarAddr);
            Assert.Empty(avatarState.inventory.Items.Where(x => x.item.Id == 400000));
            Assert.Empty(avatarState.inventory.Items.Where(x => x.item.Id == 500000));
        }
    }
}
