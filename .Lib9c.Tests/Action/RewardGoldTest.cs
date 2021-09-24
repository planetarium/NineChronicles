namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RewardGoldTest
    {
        private readonly AvatarState _avatarState;
        private readonly State _baseState;
        private readonly TableSheets _tableSheets;

        public RewardGoldTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            sheets[nameof(CharacterSheet)] = string.Join(
                    Environment.NewLine,
                    "id,_name,size_type,elemental_type,hp,atk,def,cri,hit,spd,lv_hp,lv_atk,lv_def,lv_cri,lv_hit,lv_spd,attack_range,run_speed",
                    "100010,전사,S,0,300,20,10,10,90,70,12,0.8,0.4,0,3.6,2.8,2,3");

            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();

            var avatarAddress = agentAddress.Derive("avatar");
            _tableSheets = new TableSheets(sheets);

            _avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));
            _baseState = (State)new State()
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .SetState(Addresses.GoldDistribution, GoldDistributionTest.Fixture.Select(v => v.Serialize()).Serialize())
                .MintAsset(GoldCurrencyState.Address, gold.Currency * 100000000000);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void WeeklyArenaRankingBoard(bool resetCount, bool updateNext)
        {
            var weekly = new WeeklyArenaState(0);
            weekly.Set(_avatarState, _tableSheets.CharacterSheet);
            weekly[_avatarState.address].Update(weekly[_avatarState.address], BattleLog.Result.Lose);
            var gameConfigState = new GameConfigState();
            gameConfigState.Set(_tableSheets.GameConfigSheet);
            var state = _baseState
                .SetState(weekly.address, weekly.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());
            var blockIndex = 0;

            if (resetCount)
            {
                blockIndex = gameConfigState.DailyArenaInterval;
            }

            if (updateNext)
            {
                weekly[_avatarState.address].Activate();
                blockIndex = gameConfigState.WeeklyArenaInterval;
                // Avoid NRE in test case.
                var nextWeekly = new WeeklyArenaState(1);
                state = state
                    .SetState(weekly.address, weekly.Serialize())
                    .SetState(nextWeekly.address, nextWeekly.Serialize());
            }

            Assert.False(weekly.Ended);
            Assert.Equal(4, weekly[_avatarState.address].DailyChallengeCount);

            var action = new RewardGold();

            var ctx = new ActionContext()
            {
                BlockIndex = blockIndex,
                PreviousStates = _baseState,
                Miner = default,
            };

            var states = new[]
            {
                action.WeeklyArenaRankingBoard2(ctx, state),
                action.WeeklyArenaRankingBoard(ctx, state),
            };

            foreach (var nextState in states)
            {
                var currentWeeklyState = nextState.GetWeeklyArenaState(0);
                var nextWeeklyState = nextState.GetWeeklyArenaState(1);

                Assert.Contains(WeeklyArenaState.DeriveAddress(0), nextState.UpdatedAddresses);
                Assert.Contains(WeeklyArenaState.DeriveAddress(1), nextState.UpdatedAddresses);

                if (updateNext)
                {
                    Assert.Contains(WeeklyArenaState.DeriveAddress(2), nextState.UpdatedAddresses);
                    Assert.Equal(blockIndex, nextWeeklyState.ResetIndex);
                }

                if (resetCount)
                {
                    var expectedCount = updateNext ? 4 : 5;
                    var expectedIndex = updateNext ? 0 : blockIndex;
                    Assert.Equal(expectedCount, currentWeeklyState[_avatarState.address].DailyChallengeCount);
                    Assert.Equal(expectedIndex, currentWeeklyState.ResetIndex);
                }

                Assert.Equal(updateNext, currentWeeklyState.Ended);
                Assert.Contains(_avatarState.address, currentWeeklyState);
                Assert.Equal(updateNext, nextWeeklyState.ContainsKey(_avatarState.address));
            }
        }

        [Fact]
        public void GoldDistributedEachAccount()
        {
            Currency currency = new Currency("NCG", 2, minters: null);
            Address fund = GoldCurrencyState.Address;
            Address address1 = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9");
            Address address2 = new Address("Fb90278C67f9b266eA309E6AE8463042f5461449");
            var action = new RewardGold();

            var ctx = new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _baseState,
            };

            IAccountStateDelta delta;

            // 제너시스에 받아야 할 돈들 검사
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999000000, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 1000000, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // 1번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 1;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // 3599번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 3599;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // 3600번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 3600;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999996900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 3000, delta.GetBalance(address2, currency));

            // 13600번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 13600;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999996900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 3000, delta.GetBalance(address2, currency));

            // 13601번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 13601;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // Fund 잔액을 초과해서 송금하는 경우
            // EndBlock이 긴 순서대로 송금을 진행하기 때문에, 100이 송금 성공하고 10억이 송금 실패한다.
            ctx.BlockIndex = 2;
            Assert.Throws<InsufficientBalanceException>(() =>
            {
                delta = action.GenesisGoldDistribution(ctx, _baseState);
            });
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));
        }

        [Fact]
        public void MiningReward()
        {
            Address miner = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9");
            Currency currency = _baseState.GetGoldCurrency();
            var ctx = new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _baseState,
                Miner = miner,
            };

            var action = new RewardGold();

            void AssertMinerReward(int blockIndex, string expected)
            {
                ctx.BlockIndex = blockIndex;
                IAccountStateDelta delta = action.MinerReward(ctx, _baseState);
                Assert.Equal(FungibleAssetValue.Parse(currency, expected), delta.GetBalance(miner, currency));
            }

            // Before halving (10 / 2^0 = 10)
            AssertMinerReward(0, "10");
            AssertMinerReward(1, "10");
            AssertMinerReward(12614400, "10");

            // First halving (10 / 2^1 = 5)
            AssertMinerReward(12614401, "5");
            AssertMinerReward(25228800, "5");

            // Second halving (10 / 2^2 = 2.5)
            AssertMinerReward(25228801, "2.5");
            AssertMinerReward(37843200, "2.5");

            // Third halving (10 / 2^3 = 1.25)
            AssertMinerReward(37843201, "1.25");
            AssertMinerReward(50457600, "1.25");

            // Rewardless era
            AssertMinerReward(50457601, "0");
        }
    }
}
