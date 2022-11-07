using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class EventMaterialItemRecipeSheet : Sheet<int, EventMaterialItemRecipeSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public int RequiredActionPoint { get; private set; }
            public decimal RequiredGold { get; private set; }
            public int MaterialsCount { get; private set; }
            public List<int> MaterialsId { get; private set; }
            public int ResultMaterialItemId { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                RequiredActionPoint = ParseInt(fields[1]);
                RequiredGold = ParseDecimal(fields[2]);
                MaterialsCount = ParseInt(fields[3]);
                MaterialsId = new List<int>();
                for (int i = 0; i < 12; i++)
                {
                    var id = fields[4 + i];
                    if (string.IsNullOrEmpty(id))
                        continue;

                    MaterialsId.Add(ParseInt(id));
                }
                ResultMaterialItemId = ParseInt(fields[16]);
            }
        }

        public EventMaterialItemRecipeSheet()
            : base(nameof(EventMaterialItemRecipeSheet))
        {

        }
    }
}
