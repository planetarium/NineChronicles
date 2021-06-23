namespace Lib9c.Tests.TableData.Item
{
    using Nekoyume.TableData;
    using Xunit;

    public class EquipmentItemSubRecipeSheetTest
    {
        private const string _csv = @"id,required_action_point,required_gold,required_block_index,material_id,material_count,material_2_id,material_2_count,material_3_id,material_3_count,option_id,option_ratio,option_2_id,option_2_ratio,option_3_id,option_3_ratio,option_4_id,option_4_ratio,max_option_limit
1,0,0,0,306040,1,306041,1,306023,1,1,0.74,2,0.2,3,0.06,,,1";

        [Fact]
        public void Set()
        {
            var sheet = new EquipmentItemSubRecipeSheet();
            sheet.Set(_csv);
            Assert.Single(sheet);
            Assert.NotNull(sheet.First);

            var first = sheet.First;
            Assert.Equal(first.Id, first.Key);
            Assert.Equal(1, first.Id);
            Assert.Equal(0, first.RequiredActionPoint);
            Assert.Equal(0, first.RequiredGold);
            Assert.Equal(0, first.RequiredBlockIndex);

            Assert.Equal(3, first.Materials.Count);
            Assert.Equal(306040, first.Materials[0].Id);
            Assert.Equal(1, first.Materials[0].Count);
            Assert.Equal(306041, first.Materials[1].Id);
            Assert.Equal(1, first.Materials[1].Count);
            Assert.Equal(306023, first.Materials[2].Id);
            Assert.Equal(1, first.Materials[2].Count);

            Assert.Equal(3, first.Options.Count);
            Assert.Equal(1, first.Options[0].Id);
            Assert.Equal(0.74m, first.Options[0].Ratio);
            Assert.Equal(2, first.Options[1].Id);
            Assert.Equal(0.2m, first.Options[1].Ratio);
            Assert.Equal(3, first.Options[2].Id);
            Assert.Equal(0.06m, first.Options[2].Ratio);

            Assert.Equal(1, first.MaxOptionLimit);
        }
    }
}
