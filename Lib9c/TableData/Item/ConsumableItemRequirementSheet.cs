namespace Nekoyume.TableData
{
    public class ConsumableItemRequirementSheet : Sheet<int, ConsumableItemRequirementSheet.Row>
    {
        public class Row : ItemRequirementSheet.Row
        {
        }

        public ConsumableItemRequirementSheet() : base(nameof(ConsumableItemRequirementSheet))
        {
        }
    }
}
