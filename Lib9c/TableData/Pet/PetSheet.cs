using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Pet
{
    [Serializable]
    public class PetSheet : Sheet<int, PetSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public int Id { get; private set; }
            public int Grade { get; private set; }
            public string SoulStoneTicker { get; private set; }
            public override int Key => Id;

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                Grade = ParseInt(fields[1]);
                SoulStoneTicker = fields[2];
            }
        }

        public PetSheet() : base(nameof(PetSheet))
        {
        }
    }
}
