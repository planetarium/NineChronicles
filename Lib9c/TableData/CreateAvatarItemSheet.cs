using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class CreateAvatarItemSheet: Sheet<int, CreateAvatarItemSheet.Row>
    {
        [Serializable]
        public class Row: SheetRow<int>
        {
            public override int Key => ItemId;
            public int ItemId { get; private set; }
            public int Count { get; private set; }
            public override void Set(IReadOnlyList<string> fields)
            {
                ItemId = ParseInt(fields[0]);
                Count = ParseInt(fields[1]);
            }
        }

        public CreateAvatarItemSheet() : base(nameof(CreateAvatarItemSheet))
        {
        }
    }
}
