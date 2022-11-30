namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Lib9c.Tests.Action;
    using Libplanet.Action;
    using Nekoyume;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Stat;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RankingSimulatorV1Test
    {
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;

        public RankingSimulatorV1Test()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _random = new TestRandom();
        }

        [Theory]
        [InlineData(1, 1, true)]
        [InlineData(2, 1, true)]
        [InlineData(1, 2, false)]
        public void SimulateRequiredLevel(int level, int requiredLevel, bool expected)
        {
            var rewardSheet = new WeeklyArenaRewardSheet();
            rewardSheet.Set($"id,item_id,ratio,min,max,required_level\n1,302000,0.1,1,1,{requiredLevel}");
            _tableSheets.WeeklyArenaRewardSheet = rewardSheet;
            var avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            )
            {
                level = level,
            };
            avatarState.worldInformation.ClearStage(
                1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                1,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet
            );

            var simulator = new RankingSimulatorV1(
                _random,
                avatarState,
                avatarState,
                new List<Guid>(),
                _tableSheets.GetRankingSimulatorSheetsV1(),
                1,
                new ArenaInfo(avatarState, _tableSheets.CharacterSheet, false),
                new ArenaInfo(avatarState, _tableSheets.CharacterSheet, false)
            );
            simulator.SimulateV2();

            Assert.Equal(expected, simulator.Reward.Any());
        }

        [Theory]
        [InlineData(900, 1)]
        [InlineData(1030, 2)]
        [InlineData(1150, 3)]
        [InlineData(1370, 4)]
        [InlineData(1600, 5)]
        [InlineData(1900, 6)]
        public void SimulateRankingScore(int score, int expected)
        {
            var avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            avatarState.worldInformation.ClearStage(
                1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                1,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet
            );

            var serialized = (Dictionary)new ArenaInfo(avatarState, _tableSheets.CharacterSheet, false).Serialize();
            serialized = serialized.SetItem("score", score.Serialize());
            var info = new ArenaInfo(serialized);

            var simulator = new RankingSimulatorV1(
                _random,
                avatarState,
                avatarState,
                new List<Guid>(),
                _tableSheets.GetRankingSimulatorSheetsV1(),
                1,
                info,
                new ArenaInfo(avatarState, _tableSheets.CharacterSheet, false)
            );
            simulator.SimulateV2();

            Assert.Equal(expected, simulator.Reward.Count());
        }

        [Fact]
        public void ConstructorWithCostume()
        {
            var avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            avatarState.worldInformation.ClearStage(
                1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard,
                1,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet
            );

            var enemyAvatarState = new AvatarState(avatarState);

            var row = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.ATK);
            var costume = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[row.CostumeId], _random);
            costume.equipped = true;
            avatarState.inventory.AddItem(costume);

            var row2 = _tableSheets.CostumeStatSheet.Values.First(r => r.StatType == StatType.DEF);
            var enemyCostume = (Costume)ItemFactory.CreateItem(_tableSheets.ItemSheet[row2.CostumeId], _random);
            enemyCostume.equipped = true;
            enemyAvatarState.inventory.AddItem(enemyCostume);

            var simulator = new RankingSimulatorV1(
                _random,
                avatarState,
                enemyAvatarState,
                new List<Guid>(),
                _tableSheets.GetRankingSimulatorSheetsV1(),
                1,
                new ArenaInfo(avatarState, _tableSheets.CharacterSheet, false),
                new ArenaInfo(enemyAvatarState, _tableSheets.CharacterSheet, false),
                _tableSheets.CostumeStatSheet
            );

            var player = simulator.Player;
            Assert.Equal(row.Stat, player.Stats.OptionalStats.ATK);

            var player2 = simulator.SimulateV2();
            Assert.Equal(row.Stat, player2.Stats.OptionalStats.ATK);

            var e = simulator.Log.OfType<SpawnEnemyPlayer>().First();
            var enemyPlayer = (EnemyPlayer)e.Character;
            Assert.Equal(row2.Stat, enemyPlayer.Stats.OptionalStats.DEF);
        }

        [Theory]
        [InlineData(1, 1000)]
        [InlineData(55, 1000)]
        [InlineData(85, 1000)]
        [InlineData(120, 1000)]
        [InlineData(160, 1000)]
        [InlineData(200, 1000)]
        public void CheckToReceiveAllRewardItems(int level, int simulationCount)
        {
            _tableSheets.WeeklyArenaRewardSheet = _tableSheets.WeeklyArenaRewardSheet;
            var avatarState = new AvatarState(
                default,
                default,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default)
            {
                level = level,
            };
            avatarState.worldInformation = new WorldInformation(
                0,
                _tableSheets.WorldSheet,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard);

            var simulator = new RankingSimulatorV1(
                _random,
                avatarState,
                avatarState,
                new List<Guid>(),
                _tableSheets.GetRankingSimulatorSheetsV1(),
                1,
                new ArenaInfo(avatarState, _tableSheets.CharacterSheet, false),
                new ArenaInfo(avatarState, _tableSheets.CharacterSheet, false));

            var rewardIds = new HashSet<int>();
            for (int i = 0; i < simulationCount; ++i)
            {
                simulator.SimulateV2();
                foreach (var itemBase in simulator.Reward)
                {
                    if (!rewardIds.Contains(itemBase.Id))
                    {
                        rewardIds.Add(itemBase.Id);
                    }
                }
            }

            var sheets = _tableSheets.WeeklyArenaRewardSheet.OrderedList
                .Where(x => x.Reward.RequiredLevel < level).ToList();
            var sheetIds = new HashSet<int>();
            foreach (var sheet in sheets.Where(sheet => !sheetIds.Contains(sheet.Reward.ItemId)))
            {
                sheetIds.Add(sheet.Reward.ItemId);
            }

            foreach (var id in rewardIds.TakeWhile(id => sheetIds.Count != 0).Where(id => sheetIds.Contains(id)))
            {
                sheetIds.Remove(id);
            }

            Assert.Empty(sheetIds);
        }
    }
}
