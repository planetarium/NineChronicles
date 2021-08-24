namespace Lib9c.Tests.TableData.Item
{
    using Nekoyume.Model.Stat;
    using Nekoyume.TableData;
    using Xunit;

    public class EquipmentItemOptionSheetTest
    {
        private const string _csv =
            @"id,stat_type,stat_min,stat_max,skill_id,skill_damage_min,skill_damage_max,skill_chance_min,skill_chance_max
1,ATK,6,9,,,,,
2,,,,110003,76,101,28,28";

        [Fact]
        public void Set()
        {
            var sheet = new EquipmentItemOptionSheet();
            sheet.Set(_csv);
            Assert.Equal(2, sheet.Count);
            Assert.NotNull(sheet.First);
            Assert.NotNull(sheet.Last);

            var row = sheet.First;
            Assert.Equal(row.Id, row.Key);
            Assert.Equal(1, row.Id);
            Assert.Equal(StatType.ATK, row.StatType);
            Assert.Equal(6, row.StatMin);
            Assert.Equal(9, row.StatMax);
            Assert.Equal(0, row.SkillId);
            Assert.Equal(0, row.SkillDamageMin);
            Assert.Equal(0, row.SkillDamageMax);
            Assert.Equal(0, row.SkillChanceMin);
            Assert.Equal(0, row.SkillChanceMax);

            row = sheet.Last;
            Assert.Equal(2, row.Id);
            Assert.Equal(StatType.NONE, row.StatType);
            Assert.Equal(0, row.StatMin);
            Assert.Equal(0, row.StatMax);
            Assert.Equal(110003, row.SkillId);
            Assert.Equal(76, row.SkillDamageMin);
            Assert.Equal(101, row.SkillDamageMax);
            Assert.Equal(28, row.SkillChanceMin);
            Assert.Equal(28, row.SkillChanceMax);
        }
    }
}
