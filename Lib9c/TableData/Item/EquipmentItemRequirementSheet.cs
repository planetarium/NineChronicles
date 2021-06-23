namespace Nekoyume.TableData
{
    public class EquipmentItemRequirementSheet : Sheet<int, EquipmentItemRequirementSheet.Row>
    {
        public class Row : ItemRequirementSheet.Row
        {
        }

        public EquipmentItemRequirementSheet() : base(nameof(EquipmentItemRequirementSheet))
        {
        }
    }
}
