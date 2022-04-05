using System.Collections.Generic;

namespace Nekoyume.TableData
{
    using static TableExtensions;

    public class ItemRequirementSheet : Sheet<int, ItemRequirementSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => ItemId;
            public int ItemId { get; private set; }
            public int Level { get; private set; }
            public int MimisLevel { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                ItemId = ParseInt(fields[0]);
#if UNITY_EDITOR
                Level = GameConfig.IsEditor ? 1 : ParseInt(fields[1]);
                MimisLevel = GameConfig.IsEditor ? 1 : ParseInt(fields[2]);
#else
                Level = ParseInt(fields[1]);
                MimisLevel = ParseInt(fields[2]);
#endif
            }
        }

        public ItemRequirementSheet() : base(nameof(ItemRequirementSheet))
        {
        }
    }
}
