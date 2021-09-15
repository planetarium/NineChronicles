namespace Lib9c.Tests.TableData.Item
{
    using Nekoyume.TableData;
    using Xunit;

    public class EquipmentItemSubRecipeSheetTest
    {
        private const string _csv = @"id,required_action_point,required_gold,required_block_index,material_id,material_count,material_2_id,material_2_count,material_3_id,material_3_count,option_id,option_ratio,option_2_id,option_2_ratio,option_3_id,option_3_ratio,option_4_id,option_4_ratio,max_option_limit
1,0,0,0,306040,1,306041,1,306023,1,1,0.74,2,0.2,3,0.06,,,1
2,0,150,0,306040,2,306041,1,306024,1,4,0.46,5,0.35,6,0.11,7,0.08,1";

        [Fact]
        public void Set()
        {
            var sheet = new EquipmentItemSubRecipeSheet();
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

            Assert.Equal(3, row.Options.Count);
            Assert.Equal(1, row.Options[0].Id);
            Assert.Equal(0.74m, row.Options[0].Ratio);
            Assert.Equal(2, row.Options[1].Id);
            Assert.Equal(0.2m, row.Options[1].Ratio);
            Assert.Equal(3, row.Options[2].Id);
            Assert.Equal(0.06m, row.Options[2].Ratio);

            Assert.Equal(1, row.MaxOptionLimit);

            row = sheet.Last;
            Assert.Equal(row.Id, row.Key);
            Assert.Equal(2, row.Id);
            Assert.Equal(0, row.RequiredActionPoint);
            Assert.Equal(150, row.RequiredGold);
            Assert.Equal(0, row.RequiredBlockIndex);

            Assert.Equal(3, row.Materials.Count);
            Assert.Equal(306040, row.Materials[0].Id);
            Assert.Equal(2, row.Materials[0].Count);
            Assert.Equal(306041, row.Materials[1].Id);
            Assert.Equal(1, row.Materials[1].Count);
            Assert.Equal(306024, row.Materials[2].Id);
            Assert.Equal(1, row.Materials[2].Count);

            Assert.Equal(4, row.Options.Count);
            Assert.Equal(4, row.Options[0].Id);
            Assert.Equal(0.46m, row.Options[0].Ratio);
            Assert.Equal(5, row.Options[1].Id);
            Assert.Equal(0.35m, row.Options[1].Ratio);
            Assert.Equal(6, row.Options[2].Id);
            Assert.Equal(0.11m, row.Options[2].Ratio);
            Assert.Equal(7, row.Options[3].Id);
            Assert.Equal(0.08m, row.Options[3].Ratio);

            Assert.Equal(1, row.MaxOptionLimit);
        }
    }
}
