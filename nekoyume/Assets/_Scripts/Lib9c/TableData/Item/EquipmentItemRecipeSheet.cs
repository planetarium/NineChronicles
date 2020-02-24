using System.Collections.Generic;

namespace Nekoyume.TableData
{
    public class EquipmentItemRecipeSheet : Sheet<int, EquipmentItemRecipeSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public int ResultEquipmentId { get; private set; }
            public int MaterialId { get; private set; }
            public int MaterialCount { get; private set; }
            public int RequiredActionPoint { get; private set; }
            public long RequiredGold { get; private set; }
            public int UnlockStage { get; private set; }
            public List<int> SubRecipeIds { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                ResultEquipmentId = int.Parse(fields[1]);
                MaterialId = int.Parse(fields[2]);
                MaterialCount = int.Parse(fields[3]);
                RequiredActionPoint = int.Parse(fields[4]);
                RequiredGold = long.Parse(fields[5]);
                UnlockStage = int.Parse(fields[6]);
                SubRecipeIds = new List<int>(3);
                for (var i = 7; i < 9; i++)
                {
                    if (string.IsNullOrEmpty(fields[i]))
                    {
                        break;
                    }
                    SubRecipeIds.Add(int.Parse(fields[i]));
                }
            }
        }

        public EquipmentItemRecipeSheet() : base(nameof(EquipmentItemRecipeSheet))
        {
        }
    }
}
