namespace Lib9c.Tests.Action
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RewardGoldTest : IDisposable
    {
        private readonly AvatarState _avatarState;
        private TableSheets _tableSheets;
        private State _baseState;

        public RewardGoldTest()
        {
            _tableSheets = new TableSheets();
            _tableSheets.SetToSheet(nameof(WorldSheet), "test");
            _tableSheets.SetToSheet(nameof(QuestSheet), "test");
            _tableSheets.SetToSheet(nameof(QuestRewardSheet), "test");
            _tableSheets.SetToSheet(nameof(QuestItemRewardSheet), "test");
            _tableSheets.SetToSheet(nameof(EquipmentItemRecipeSheet), "test");
            _tableSheets.SetToSheet(nameof(EquipmentItemSubRecipeSheet), "test");
            _tableSheets.SetToSheet(
                nameof(CharacterSheet),
                "id,_name,size_type,elemental_type,hp,atk,def,cri,hit,spd,lv_hp,lv_atk,lv_def,lv_cri,lv_hit,lv_spd,attack_range,run_speed\n100010,전사,S,0,300,20,10,10,90,70,12,0.8,0.4,0,3.6,2.8,2,3");

            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();

            var avatarAddress = agentAddress.Derive("avatar");
            _avatarState = new AvatarState(avatarAddress, agentAddress, 0, _tableSheets, new GameConfigState());

            var gold = new GoldCurrencyState(new Currency("NCG", minter: null));
            _baseState = (State)new State()
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .SetState(Addresses.GoldDistribution, GoldDistributionTest.Fixture.Select(v => v.Serialize()).Serialize())
                .MintAsset(GoldCurrencyState.Address, gold.Currency, 100000000000);
        }

        public void Dispose()
        {
            _tableSheets = null;
        }

        [Fact]
        public void ExecuteCreateNextWeeklyArenaState()
        {
            var weekly = new WeeklyArenaState(0);
            var state = _baseState
                .SetState(weekly.address, weekly.Serialize());

            var action = new RewardGold();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Miner = default,
                BlockIndex = 1,
            });

            nextState.TryGetGoldBalance(default, out var reward);

            Assert.Equal(1, reward);
            Assert.Contains(WeeklyArenaState.DeriveAddress(1), nextState.UpdatedAddresses);
        }

        [Fact]
        public void ExecuteResetCount()
        {
            var weekly = new WeeklyArenaState(0);
            weekly.Set(_avatarState, _tableSheets.CharacterSheet);
            weekly[_avatarState.address].Update(_avatarState, weekly[_avatarState.address], BattleLog.Result.Lose);

            Assert.Equal(4, weekly[_avatarState.address].DailyChallengeCount);

            var state = _baseState.SetState(weekly.address, weekly.Serialize());
            var action = new RewardGold();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Miner = default,
                BlockIndex = GameConfig.DailyArenaInterval,
            });

            var current = nextState.GetWeeklyArenaState(0);
            nextState.TryGetGoldBalance(default, out var reward);

            Assert.Equal(1, reward);
            Assert.Contains(WeeklyArenaState.DeriveAddress(1), nextState.UpdatedAddresses);
            Assert.Equal(GameConfig.DailyArenaInterval, current.ResetIndex);
            Assert.Equal(5, current[_avatarState.address].DailyChallengeCount);
        }

        [Fact]
        public void ExecuteUpdateNextWeeklyArenaState()
        {
            var prevWeekly = new WeeklyArenaState(0);
            prevWeekly.Set(_avatarState, _tableSheets.CharacterSheet);
            prevWeekly[_avatarState.address].Activate();

            Assert.False(prevWeekly.Ended);
            Assert.True(prevWeekly[_avatarState.address].Active);

            var weekly = new WeeklyArenaState(1);
            var state = _baseState
                .SetState(prevWeekly.address, prevWeekly.Serialize())
                .SetState(weekly.address, weekly.Serialize());

            var action = new RewardGold();

            var nextState = action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Miner = default,
                BlockIndex = GameConfig.WeeklyArenaInterval,
            });

            nextState.TryGetGoldBalance(default, out var reward);
            var prev = nextState.GetWeeklyArenaState(0);
            var current = nextState.GetWeeklyArenaState(1);

            Assert.Equal(1, reward);
            Assert.Equal(prevWeekly.address, prev.address);
            Assert.Equal(weekly.address, current.address);
            Assert.True(prev.Ended);
            Assert.Equal(GameConfig.WeeklyArenaInterval, current.ResetIndex);
            Assert.Contains(_avatarState.address, current);
        }

        [Fact]
        public void GoldDistributedEachAccount()
        {
            Currency currency = new Currency("NCG", minters: null);
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
            Assert.Equal(99999000000, delta.GetBalance(fund, currency));
            Assert.Equal(1000000, delta.GetBalance(address1, currency));
            Assert.Equal(0, delta.GetBalance(address2, currency));

            // 1번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 1;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(100, delta.GetBalance(address1, currency));
            Assert.Equal(0, delta.GetBalance(address2, currency));

            // 3599번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 3599;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(100, delta.GetBalance(address1, currency));
            Assert.Equal(0, delta.GetBalance(address2, currency));

            // 3600번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 3600;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(99999996900, delta.GetBalance(fund, currency));
            Assert.Equal(100, delta.GetBalance(address1, currency));
            Assert.Equal(3000, delta.GetBalance(address2, currency));

            // 13600번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 13600;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(99999996900, delta.GetBalance(fund, currency));
            Assert.Equal(100, delta.GetBalance(address1, currency));
            Assert.Equal(3000, delta.GetBalance(address2, currency));

            // 13601번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 13601;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(100, delta.GetBalance(address1, currency));
            Assert.Equal(0, delta.GetBalance(address2, currency));

            // Fund 잔액을 초과해서 송금하는 경우
            // EndBlock이 긴 순서대로 송금을 진행하기 때문에, 100이 송금 성공하고 10억이 송금 실패한다.
            ctx.BlockIndex = 2;
            Assert.Throws<InsufficientBalanceException>(() =>
            {
                delta = action.GenesisGoldDistribution(ctx, _baseState);
            });
            Assert.Equal(99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(100, delta.GetBalance(address1, currency));
            Assert.Equal(0, delta.GetBalance(address2, currency));
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

            IAccountStateDelta delta;

            // 반감기가 오기 전 마이닝 보상
            ctx.BlockIndex = 1;
            delta = action.MinerReward(ctx, _baseState);
            Assert.Equal(10, delta.GetBalance(miner, currency));

            // 첫 번째 반감기
            ctx.BlockIndex = 12614400;
            delta = action.MinerReward(ctx, _baseState);
            Assert.Equal(5, delta.GetBalance(miner, currency));

            // 두 번째 반감기
            ctx.BlockIndex = 25228880;
            delta = action.MinerReward(ctx, _baseState);
            Assert.Equal(3, delta.GetBalance(miner, currency));
        }
    }

    public class GoldDistributionTest
    {
        public static readonly GoldDistribution[] Fixture =
        {
            new GoldDistribution
            {
                Address = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                AmountPerBlock = 100,
                StartBlock = 1,
                EndBlock = 100000,
            },
            new GoldDistribution
            {
                Address = new Address("Fb90278C67f9b266eA309E6AE8463042f5461449"),
                AmountPerBlock = 3000,
                StartBlock = 3600,
                EndBlock = 13600,
            },
            new GoldDistribution
            {
                Address = new Address("Fb90278C67f9b266eA309E6AE8463042f5461449"),
                AmountPerBlock = 100000000000,
                StartBlock = 2,
                EndBlock = 2,
            },
            new GoldDistribution
            {
                Address = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9"),
                AmountPerBlock = 1000000,
                StartBlock = 0,
                EndBlock = 0,
            },
        };

        public static string CreateFixtureCsvFile()
        {
            string csvPath = Path.GetTempFileName();
            using (StreamWriter writer = File.CreateText(csvPath))
            {
                writer.Write(@"Address,AmountPerBlock,StartBlock,EndBlock
F9A15F870701268Bd7bBeA6502eB15F4997f32f9,1000000,0,0
F9A15F870701268Bd7bBeA6502eB15F4997f32f9,100,1,100000
Fb90278C67f9b266eA309E6AE8463042f5461449,3000,3600,13600
Fb90278C67f9b266eA309E6AE8463042f5461449,100000000000,2,2
");
            }

            return csvPath;
        }

        [Fact]
        public void LoadInDescendingEndBlockOrder()
        {
            string fixturePath = CreateFixtureCsvFile();
            GoldDistribution[] records = GoldDistribution.LoadInDescendingEndBlockOrder(fixturePath);
            Assert.Equal(Fixture, records);
        }
    }
}
