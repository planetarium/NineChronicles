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

    public class StageSimulatorTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;

        public StageSimulatorTest()
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
        public void Simulate()
        {
            var row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[row.CostumeId], _random);
            costume.equipped = true;
            _avatarState.inventory.AddItem(costume);

            var simulator = new StageSimulator(
                _random,
                _avatarState,
                new List<Guid>(),
                null,
                new List<Nekoyume.Model.Skill.Skill>(),
                1,
                1,
                _tableSheets.StageSheet[1],
                _tableSheets.StageWaveSheet[1],
                false,
                20,
                _tableSheets.GetSimulatorSheets(),
                _tableSheets.EnemySkillSheet,
                _tableSheets.CostumeStatSheet,
                StageSimulator.GetWaveRewards(
                    _random,
                    _tableSheets.StageSheet[1],
                    _tableSheets.MaterialItemSheet)
            );

            var player = simulator.Player;
            Assert.Equal(row.Stat, player.Stats.OptionalStats.ATK);
            while (player.Level == 1)
            {
                simulator.Simulate();
            }

            var player2 = simulator.Player;
            Assert.Equal(row.Stat, player2.Stats.OptionalStats.ATK);
            Assert.Equal(2, player2.Level);
            Assert.Equal(1, player2.eventMap[(int)QuestEventType.Level]);
            Assert.True(simulator.Log.OfType<GetExp>().Any());

            var filtered =
                simulator.Log
                    .Select(e => e.GetType())
                    .Where(type =>
                        type != typeof(GetReward) ||
                        type != typeof(DropBox));
            Assert.Equal(typeof(WaveTurnEnd), filtered.Last());
            Assert.Equal(1, simulator.Log.OfType<WaveTurnEnd>().First().TurnNumber);
        }
    }
}
