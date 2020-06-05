using System;
using System.Collections.Generic;
using System.Linq;
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
            public List<int> MaterialItemIds { get; private set; }
            public int ResultConsumableItemId { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                RequiredBlockIndex = ParseLong(fields[1]);
                RequiredActionPoint = ParseInt(fields[2]);
                RequiredGold = ParseDecimal(fields[3]);
                MaterialItemIds = new List<int>();
                for (var i = 4; i < 8; i++)
                {
                    if (string.IsNullOrEmpty(fields[i]))
                        break;

                    MaterialItemIds.Add(ParseInt(fields[i]));
                }
                MaterialItemIds.Sort((left, right) => left - right);

                ResultConsumableItemId = ParseInt(fields[8]);
            }

            public bool IsMatch(IEnumerable<int> materialItemIds)
            {
                var itemIds = materialItemIds as int[] ?? materialItemIds.ToArray();
                return MaterialItemIds.Count == itemIds.Length &&
                    MaterialItemIds.All(itemIds.Contains);
            }
        }

        public ConsumableItemRecipeSheet() : base(nameof(ConsumableItemRecipeSheet))
        {
        }

        public bool TryGetValue(IEnumerable<int> materialItemIds, out Row row, bool throwException = false)
        {
            foreach (var value in Values.Where(value => value.IsMatch(materialItemIds)))
            {
                row = value;
                return true;
            }

            row = null;
            return false;
        }
    }
}
