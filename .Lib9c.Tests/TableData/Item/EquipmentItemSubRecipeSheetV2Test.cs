namespace Lib9c.Tests.TableData.Item
{
    using Nekoyume.TableData;
    using Xunit;

    public class EquipmentItemSubRecipeSheetV2Test
    {
        private const string _csv = @"id,required_action_point,required_gold,required_block_index,material_id,material_count,material_2_id,material_2_count,material_3_id,material_3_count,option_id,option_ratio,option_1_required_block_index,option_2_id,option_2_ratio,option_2_required_block_index,option_3_id,option_3_ratio,option_3_required_block_index,option_4_id,option_4_ratio,option_4_required_block_index
1,0,0,0,306040,1,306041,1,306023,1,1,1,0,2,2800,200,3,1800,300,7,1200,600
2,0,450,240,306056,15,306061,8,306068,6,8,1,0,9,3800,200,10,900,300,11,500,600";

        [Fact]
        public void Set()
        {
            var sheet = new EquipmentItemSubRecipeSheetV2();
            sheet.Set(_csv);
            Assert.Equal(2, sheet.Count);
            Assert.NotNull(sheet.First);
            Assert.NotNull(sheet.Last);

            var row = sheet.First;
            Assert.Equal(row.Id, row.Key);
            Assert.Equal(1, row.Id);
            Assert.Equal(0, row.RequiredActionPoint);
            Assert.Equal(0, row.RequiredGold);
            Assert.Equal(0, row.RequiredBlockIndex);

            Assert.Equal(3, row.Materials.Count);
            Assert.Equal(306040, row.Materials[0].Id);
            Assert.Equal(1, row.Materials[0].Count);
            Assert.Equal(306041, row.Materials[1].Id);
            Assert.Equal(1, row.Materials[1].Count);
            Assert.Equal(306023, row.Materials[2].Id);
            Assert.Equal(1, row.Materials[2].Count);

            Assert.Equal(4, row.Options.Count);
            Assert.Equal(1, row.Options[0].Id);
            Assert.Equal(1, row.Options[0].Ratio);
            Assert.Equal(0, row.Options[0].RequiredBlockIndex);
            Assert.Equal(2, row.Options[1].Id);
            Assert.Equal(2800, row.Options[1].Ratio);
            Assert.Equal(200, row.Options[1].RequiredBlockIndex);
            Assert.Equal(3, row.Options[2].Id);
            Assert.Equal(1800, row.Options[2].Ratio);
            Assert.Equal(300, row.Options[2].RequiredBlockIndex);
            Assert.Equal(7, row.Options[3].Id);
            Assert.Equal(1200, row.Options[3].Ratio);
            Assert.Equal(600, row.Options[3].RequiredBlockIndex);

            row = sheet.Last;
            Assert.Equal(row.Id, row.Key);
            Assert.Equal(2, row.Id);
            Assert.Equal(0, row.RequiredActionPoint);
            Assert.Equal(450, row.RequiredGold);
            Assert.Equal(240, row.RequiredBlockIndex);

            Assert.Equal(3, row.Materials.Count);
            Assert.Equal(306056, row.Materials[0].Id);
            Assert.Equal(15, row.Materials[0].Count);
            Assert.Equal(306061, row.Materials[1].Id);
            Assert.Equal(8, row.Materials[1].Count);
            Assert.Equal(306068, row.Materials[2].Id);
            Assert.Equal(6, row.Materials[2].Count);

            Assert.Equal(4, row.Options.Count);
            Assert.Equal(8, row.Options[0].Id);
            Assert.Equal(1, row.Options[0].Ratio);
            Assert.Equal(0, row.Options[0].RequiredBlockIndex);
            Assert.Equal(9, row.Options[1].Id);
            Assert.Equal(3800, row.Options[1].Ratio);
            Assert.Equal(200, row.Options[1].RequiredBlockIndex);
            Assert.Equal(10, row.Options[2].Id);
            Assert.Equal(900, row.Options[2].Ratio);
            Assert.Equal(300, row.Options[2].RequiredBlockIndex);
            Assert.Equal(11, row.Options[3].Id);
            Assert.Equal(500, row.Options[3].Ratio);
            Assert.Equal(600, row.Options[3].RequiredBlockIndex);
        }
    }
}
