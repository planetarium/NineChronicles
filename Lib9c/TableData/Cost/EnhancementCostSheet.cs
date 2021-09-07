using System;
using System.Collections.Generic;
using System.Numerics;
using Nekoyume.Model.Item;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class EnhancementCostSheet : Sheet<int, EnhancementCostSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public ItemSubType ItemSubType { get; private set; }
            public int Grade { get; private set; }
            public int Level { get; private set; }
            public BigInteger Cost { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ItemSubType = (ItemSubType) Enum.Parse(typeof(ItemSubType), fields[1]);
                Grade = ParseInt(fields[2]);
                Level = ParseInt(fields[3]);
                Cost = ParseInt(fields[4]);
            }
        }

        public EnhancementCostSheet() : base(nameof(EnhancementCostSheet))
        {
        }
    }
}
