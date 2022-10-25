namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Quest;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Xunit;

    public class StageSimulatorV1Test
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;

        public StageSimulatorV1Test()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _random = new TestRandom();

            _avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
        }

        [Fact]
        public void Simulate3()
        {
            var simulator = new StageSimulatorV1(
                _random,
                _avatarState,
                new List<Guid>(),
                1,
                3,
                _tableSheets.GetStageSimulatorSheetsV1(),
                2,
                1);
            simulator.SimulateV2();
            var filtered =
                simulator.Log.Where(e => e.GetType() != typeof(GetReward) || e.GetType() != typeof(DropBox));
            Assert.Equal(typeof(WaveTurnEnd), filtered.Last().GetType());
            Assert.Equal(1, simulator.Log.OfType<WaveTurnEnd>().First().TurnNumber);
        }

        [Fact]
        public void ConstructorWithCostume()
        {
            var row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[row.CostumeId], _random);
            costume.equipped = true;
            _avatarState.inventory.AddItem(costume);

            var simulator = new StageSimulatorV1(
                _random,
                _avatarState,
                new List<Guid>(),
                1,
                1,
                _tableSheets.GetStageSimulatorSheetsV1(),
                _tableSheets.CostumeStatSheet,
                2);

            var player = simulator.Player;
            Assert.Equal(row.Stat, player.Stats.OptionalStats.ATK);

            var player2 = simulator.SimulateV2();
            Assert.Equal(row.Stat, player2.Stats.OptionalStats.ATK);
        }

        [Fact]
        public void Simulate5()
        {
            var simulator = new StageSimulatorV1(
                _random,
                _avatarState,
                new List<Guid>(),
                1,
                1,
                _tableSheets.GetStageSimulatorSheetsV1(),
                _tableSheets.CostumeStatSheet,
                2);

            var player = simulator.Player;

            while (player.Level == 1)
            {
                simulator.SimulateV4();
            }

            var player2 = simulator.Player;
            Assert.Equal(2, player2.Level);
            Assert.Equal(1, player2.eventMap[(int)QuestEventType.Level]);
            Assert.True(simulator.Log.OfType<GetExp>().Any());
        }

        [Fact]
        public void Simulate6()
        {
            var simulator = new StageSimulatorV1(
                _random,
                _avatarState,
                new List<Guid>(),
                1,
                1,
                _tableSheets.GetStageSimulatorSheetsV1(),
                _tableSheets.CostumeStatSheet,
                2);

            var player = simulator.Player;

            while (player.Level == 1)
            {
                simulator.SimulateV5(1);
            }

            var player2 = simulator.Player;
            Assert.Equal(2, player2.Level);
            Assert.Equal(1, player2.eventMap[(int)QuestEventType.Level]);
            Assert.True(simulator.Log.OfType<GetExp>().Any());
        }

        [Fact]
        public void Simulate()
        {
            var simulator = new StageSimulatorV1(
                _random,
                _avatarState,
                new List<Guid>(),
                1,
                1,
                _tableSheets.GetStageSimulatorSheetsV1(),
                _tableSheets.CostumeStatSheet,
                2);

            var player = simulator.Player;

            while (player.Level == 1)
            {
                simulator.Simulate(1);
            }

            var player2 = simulator.Player;
            Assert.Equal(2, player2.Level);
            Assert.Equal(1, player2.eventMap[(int)QuestEventType.Level]);
            Assert.True(simulator.Log.OfType<GetExp>().Any());
        }
    }
}
