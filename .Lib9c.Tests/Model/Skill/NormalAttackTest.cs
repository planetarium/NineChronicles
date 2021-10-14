namespace Lib9c.Tests.Model.Skill
{
    using System.Collections.Generic;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Battle;
    using Nekoyume.Model;
    using Nekoyume.Model.Buff;
    using Nekoyume.Model.Skill;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class NormalAttackTest
    {
        public NormalAttackTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        [Fact]
        public void Use()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);

            Assert.True(tableSheets.SkillSheet.TryGetValue(100000, out var skillRow));
            var normalAttack = new NormalAttack(skillRow, 100, 100);

            var avatarState = new AvatarState(
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                new PrivateKey().ToAddress());

            var worldRow = tableSheets.WorldSheet.First;
            Assert.NotNull(worldRow);

            var simulator = new StageSimulator(
                new TestRandom(),
                avatarState,
                null,
                worldRow.Id,
                worldRow.StageBegin,
                tableSheets.GetStageSimulatorSheets(),
                2,
                1);
            var player = new Player(avatarState, simulator);

            var enemyRow = tableSheets.CharacterSheet.OrderedList
                .FirstOrDefault(e => e.Id > 200000);
            Assert.NotNull(enemyRow);

            var enemy = new Enemy(player, enemyRow, 1);

            player.Targets.Add(enemy);
            var battleStatusSkill = normalAttack.Use(
                player,
                0,
                new List<Buff>());
            Assert.NotNull(battleStatusSkill);
            Assert.Single(battleStatusSkill.SkillInfos);

            var skillInfo = battleStatusSkill.SkillInfos.FirstOrDefault();
            Assert.NotNull(skillInfo);
            Assert.Equal(enemy.Id, skillInfo.Target.Id);
        }
    }
}
