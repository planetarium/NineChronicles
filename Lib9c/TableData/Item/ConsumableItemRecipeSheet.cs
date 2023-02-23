using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class ConsumableItemRecipeSheet : Sheet<int, ConsumableItemRecipeSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public long RequiredBlockIndex { get; private set; }
            public int RequiredActionPoint { get; private set; }
            public decimal RequiredGold { get; private set; }
            public List<EquipmentItemSubRecipeSheet.MaterialInfo> Materials { get; private set; }
            public int ResultConsumableItemId { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                RequiredBlockIndex = ParseLong(fields[1]);
                RequiredActionPoint = ParseInt(fields[2]);
                RequiredGold = ParseDecimal(fields[3]);
                Materials = new List<EquipmentItemSubRecipeSheet.MaterialInfo>();
                for (var i = 0; i < 4; i++)
                {
                    var offset = i * 2;
                    var id = fields[4 + offset];
                    var count = fields[5 + offset];
                    if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(count))
                        continue;

                    Materials.Add(new EquipmentItemSubRecipeSheet.MaterialInfo(ParseInt(id), ParseInt(count)));
                }

                ResultConsumableItemId = ParseInt(fields[12]);
            }
        }

        public ConsumableItemRecipeSheet() : base(nameof(ConsumableItemRecipeSheet))
        {
        }
    }
}
