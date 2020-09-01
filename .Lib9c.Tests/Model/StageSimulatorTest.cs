namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class StageSimulatorTest
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly AvatarState _avatarState;

        public StageSimulatorTest()
        {
            _tableSheets = TableSheets.FromTableSheetsState(TableSheetsImporter.ImportTableSheets());
            _random = new ItemEnhancementTest.TestRandom();
            _avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.WorldSheet,
                _tableSheets.QuestSheet,
                _tableSheets.QuestRewardSheet,
                _tableSheets.QuestItemRewardSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheet,
                new GameConfigState()
            );
        }

        [Fact]
        public void Simulate()
        {
            var simulator = new StageSimulator(
                _random,
                _avatarState,
                new List<Guid>(),
                1,
                3,
                _tableSheets.MaterialItemSheet,
                _tableSheets.SkillSheet,
                _tableSheets.SkillBuffSheet,
                _tableSheets.BuffSheet,
                _tableSheets.CharacterSheet,
                _tableSheets.CharacterLevelSheet,
                _tableSheets.EquipmentItemSetEffectSheet,
                _tableSheets.StageSheet,
                _tableSheets.StageWaveSheet,
                _tableSheets.EnemySkillSheet
            );
            simulator.Simulate();
            var filtered =
                simulator.Log.Where(e => e.GetType() != typeof(GetReward) || e.GetType() != typeof(DropBox));
            Assert.Equal(typeof(WaveTurnEnd), filtered.Last().GetType());
        }
    }
}
