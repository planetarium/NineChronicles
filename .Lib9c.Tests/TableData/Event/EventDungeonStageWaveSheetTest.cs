namespace Lib9c.Tests.TableData.Event
{
    using Nekoyume.TableData.Event;
    using Xunit;

    public class EventDungeonStageWaveSheetTest
    {
        [Fact]
        public void Set()
        {
            const string csv = @"event_dungeon_stage_id,wave,monster1_id,monster1_level,monster1_count,monster2_id,monster2_level,monster2_count,monster3_id,monster3_level,monster3_count,monster4_id,monster4_level,monster4_count,has_boss
10010001,1,204010,1,1,,,,,,,,,,0
10010001,2,204010,1,1,,,,,,,,,,0
10010001,3,204010,1,1,,,,,,,,,,0";

            var sheet = new EventDungeonStageWaveSheet();
            sheet.Set(csv);
            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            Assert.NotNull(sheet.Last);
            var row = sheet.First;
            Assert.Equal(10010001, row.StageId);
            Assert.Equal(3, row.Waves.Count);
            var wave = row.Waves[0];
            Assert.Equal(1, wave.Number);
            Assert.Single(wave.Monsters);
            var monster = wave.Monsters[0];
            Assert.Equal(204010, monster.CharacterId);
            Assert.Equal(1, monster.Level);
            Assert.Equal(1, monster.Count);
            wave = row.Waves[1];
            Assert.Equal(2, wave.Number);
            Assert.Single(wave.Monsters);
            monster = wave.Monsters[0];
            Assert.Equal(204010, monster.CharacterId);
            Assert.Equal(1, monster.Level);
            Assert.Equal(1, monster.Count);
            wave = row.Waves[2];
            Assert.Equal(3, wave.Number);
            Assert.Single(wave.Monsters);
            monster = wave.Monsters[0];
            Assert.Equal(204010, monster.CharacterId);
            Assert.Equal(1, monster.Level);
            Assert.Equal(1, monster.Count);
            Assert.False(row.HasBoss);
            Assert.Single(row.TotalMonsterIds);
        }
    }
}
