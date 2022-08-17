namespace Lib9c.Tests.TableData.Event
{
    using System.Text;
    using Nekoyume.TableData.Event;
    using Xunit;

    public class EventConsumableItemRecipeSheetTest
    {
        [Fact]
        public void Set()
        {
            var sb = new StringBuilder();
            sb.AppendLine("id,required_block_index,required_ap,required_gold,material_item_id_1,material_item_count_1,material_item_id_2,material_item_count_2,material_item_id_3,material_item_count_3,material_item_id_4,material_item_count_4,result_consumable_item_id");
            sb.AppendLine("10010001,20,0,0,302001,2,302002,1,,,,,201000");
            var csv = sb.ToString();

            var sheet = new EventConsumableItemRecipeSheet();
            sheet.Set(csv);
            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            Assert.NotNull(sheet.Last);
            var row = sheet.First;
            Assert.Equal(10010001, row.Id);
            Assert.Equal(20, row.RequiredBlockIndex);
            Assert.Equal(0, row.RequiredActionPoint);
            Assert.Equal(0, row.RequiredGold);
            Assert.Equal(2, row.Materials.Count);
            var materialInfo = row.Materials[0];
            Assert.Equal(302001, materialInfo.Id);
            Assert.Equal(2, materialInfo.Count);
            materialInfo = row.Materials[1];
            Assert.Equal(302002, materialInfo.Id);
            Assert.Equal(1, materialInfo.Count);
            Assert.Equal(201000, row.ResultConsumableItemId);
        }
    }
}
