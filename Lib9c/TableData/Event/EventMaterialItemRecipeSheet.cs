using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Event
{
    [Serializable]
    public class EventMaterialItemRecipeSheet : Sheet<int, EventMaterialItemRecipeSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public int ResultMaterialItemId { get; private set; }
            public int ResultMaterialItemCount { get; private set; }
            public int RequiredMaterialsCount { get; private set; }
            public List<int> RequiredMaterialsId { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ResultMaterialItemId = ParseInt(fields[1]);
                ResultMaterialItemCount = ParseInt(fields[2]);
                RequiredMaterialsCount = ParseInt(fields[3]);
                RequiredMaterialsId = new List<int>();
                const int maxCountOfMaterialTypes = 12;
                const int offset = 4;
                for (int i = 0; i < maxCountOfMaterialTypes ; i++)
                {
                    var id = fields[offset + i];
                    if (string.IsNullOrEmpty(id))
                        continue;

                    RequiredMaterialsId.Add(ParseInt(id));
                }
            }
        }

        public EventMaterialItemRecipeSheet()
            : base(nameof(EventMaterialItemRecipeSheet))
        {
        }
    }
}
