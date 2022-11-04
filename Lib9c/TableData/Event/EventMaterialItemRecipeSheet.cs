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
            public int ResultMaterialItemId { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                RequiredActionPoint = ParseInt(fields[1]);
                RequiredGold = ParseDecimal(fields[2]);
                ResultMaterialItemId = ParseInt(fields[3]);
            }
        }

        public EventMaterialItemRecipeSheet()
            : base(nameof(EventMaterialItemRecipeSheet))
        {

        }
    }
}
