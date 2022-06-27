namespace Lib9c.Tests.TableData.Item
{
    using Nekoyume.Model.Item;
    using Nekoyume.TableData;
    using Xunit;

    public class EquipmentItemRecipeSheetTest
    {
        [InlineData(
            @"id,result_equipment_id,material_id,material_count,required_action_point,required_gold,required_block_index,unlock_stage,sub_recipe_id,sub_recipe_id_2,sub_recipe_id_3
1,10110000,303000,2,0,0,5,3,373,374,375",
            0,
            null
        )]
        [InlineData(
            @"id,result_equipment_id,material_id,material_count,required_action_point,required_gold,required_block_index,unlock_stage,sub_recipe_id,sub_recipe_id_2,sub_recipe_id_3,required_crystal,item_sub_type
1,10110000,303000,2,0,0,5,3,373,374,375,100,Weapon",
            100,
            ItemSubType.Weapon
        )]
        [Theory]
        public void Set(string csv, int expected, ItemSubType? expectedItemSubType)
        {
            var sheet = new EquipmentItemRecipeSheet();
            sheet.Set(csv);

            Assert.Single(sheet);

            EquipmentItemRecipeSheet.Row row = sheet.First!;

            Assert.Equal(expected, row.CRYSTAL);
            Assert.Equal(expectedItemSubType, row.ItemSubType);
        }
    }
}
