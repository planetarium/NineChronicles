namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Immutable;
    using Bencodex.Types;
    using Libplanet;
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
                .MintAsset(GoldCurrencyState.Address, gold.Currency, 100000);
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

            var action = new RewardGold()
            {
                Gold = 1,
            };

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
            var action = new RewardGold()
            {
                Gold = 1,
            };

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

            var action = new RewardGold()
            {
                Gold = 1,
            };
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
    }
}
