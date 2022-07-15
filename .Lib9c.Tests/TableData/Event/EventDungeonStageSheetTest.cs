namespace Lib9c.Tests.TableData.Event
{
    using Nekoyume.TableData.Event;
    using Xunit;

    public class EventDungeonStageSheetTest
    {
        [Fact]
        public void Set()
        {
            const string csv = @"id,cost_ap,turn_limit,hp_option,atk_option,def_option,cri_option,hit_option,spd_option,background,bgm,item1,item1_ratio,item1_min,item1_max,item2,item2_ratio,item2_min,item2_max,item3,item3_ratio,item3_min,item3_max,item4,item4_ratio,item4_min,item4_max,item5,item5_ratio,item5_min,item5_max,item6,item6_ratio,item6_min,item6_max,item7,item7_ratio,item7_min,item7_max,item8,item8_ratio,item8_min,item8_max,item9,item9_ratio,item9_min,item9_max,item10,item10_ratio,item10_min,item10_max,min_drop,max_drop
10010001,5,150,0,0,0,0,0,0,chapter_01_01,bgm_yggdrasil_01,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,0,0";

            var sheet = new EventDungeonStageSheet();
            sheet.Set(csv);
            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            Assert.NotNull(sheet.Last);
            var row = sheet.First;
            Assert.Equal(10010001, row.Id);
            Assert.Equal(5, row.CostAP);
            Assert.Equal(150, row.TurnLimit);
            Assert.Empty(row.EnemyOptionalStatModifiers);
            Assert.Equal("chapter_01_01", row.Background);
            Assert.Equal("bgm_yggdrasil_01", row.BGM);
            Assert.Empty(row.Rewards);
            Assert.Equal(0, row.DropItemMin);
            Assert.Equal(0, row.DropItemMax);
        }
    }
}
