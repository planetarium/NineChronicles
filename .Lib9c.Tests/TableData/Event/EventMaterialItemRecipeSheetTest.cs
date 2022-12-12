namespace Lib9c.Tests.TableData.Event
{
    using System.Text;
    using Nekoyume.TableData.Event;
    using Xunit;

    public class EventMaterialItemRecipeSheetTest
    {
        [Fact]
        public void Set()
        {
            var sb = new StringBuilder();
            sb.AppendLine("id,result_consumable_item_id,result_consumable_item_count,material_item_count,material_item_id_1,material_item_id_2,material_item_id_3,material_item_id_4,material_item_id_5,material_item_id_6,material_item_id_7,material_item_id_8,material_item_id_9,material_item_id_10,material_item_id_11,material_item_id_12");
            sb.AppendLine("10020001,500000,3,15,700000,700001,700002,700102,700104,700106,700202,700204,,,,");
            var csv = sb.ToString();

            var sheet = new EventMaterialItemRecipeSheet();
            sheet.Set(csv);
            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            Assert.NotNull(sheet.Last);
            var row = sheet.First;
            Assert.Equal(10020001, row.Id);
            Assert.Equal(500000, row.ResultMaterialItemId);
            Assert.Equal(3, row.ResultMaterialItemCount);
            Assert.Equal(15, row.RequiredMaterialsCount);
            Assert.Equal(8, row.RequiredMaterialsId.Count);
            Assert.Equal(700000, row.RequiredMaterialsId[0]);
            Assert.Equal(700001, row.RequiredMaterialsId[1]);
            Assert.Equal(700002, row.RequiredMaterialsId[2]);
            Assert.Equal(700102, row.RequiredMaterialsId[3]);
            Assert.Equal(700104, row.RequiredMaterialsId[4]);
            Assert.Equal(700106, row.RequiredMaterialsId[5]);
            Assert.Equal(700202, row.RequiredMaterialsId[6]);
            Assert.Equal(700204, row.RequiredMaterialsId[7]);
        }
    }
}
