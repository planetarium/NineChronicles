using System;
using System.Collections.Generic;
using Nekoyume.Model.Item;
using static Nekoyume.TableData.TableExtensions;

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
            public long RequiredBlockIndex { get; private set; }
            public int UnlockStage { get; private set; }
            public List<int> SubRecipeIds { get; private set; }
            public int CRYSTAL { get; private set; }
            public ItemSubType? ItemSubType { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ResultEquipmentId = ParseInt(fields[1]);
                MaterialId = ParseInt(fields[2]);
                MaterialCount = ParseInt(fields[3]);
                RequiredActionPoint = ParseInt(fields[4]);
                RequiredGold = ParseLong(fields[5]);
                RequiredBlockIndex = ParseLong(fields[6]);
                UnlockStage = ParseInt(fields[7]);
                SubRecipeIds = new List<int>(3);
                for (var i = 8; i < 11; i++)
                {
                    if (string.IsNullOrEmpty(fields[i]))
                    {
                        break;
                    }
                    SubRecipeIds.Add(ParseInt(fields[i]));
                }

                CRYSTAL = 0;
                if (fields.Count >= 12)
                {
                    CRYSTAL = ParseInt(fields[11]);
                }

                if (fields.Count >= 13)
                {
                    ItemSubType = (ItemSubType) Enum.Parse(typeof(ItemSubType), fields[12]);
                }
            }

            public bool IsMimisBrunnrSubRecipe(int? subRecipeId)
            {
                return subRecipeId.HasValue && SubRecipeIds[2] == subRecipeId.Value;
            }
        }

        public EquipmentItemRecipeSheet() : base(nameof(EquipmentItemRecipeSheet))
        {
        }
    }
}
