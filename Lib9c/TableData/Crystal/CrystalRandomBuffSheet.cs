using System;
using System.Collections.Generic;
using Nekoyume.Model.Buff;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Crystal
{
    [Serializable]
    public class CrystalRandomBuffSheet : Sheet<int, CrystalRandomBuffSheet.Row>
    {
        public class Row : SheetRow<int>
        {
            public enum BuffRank
            {
                SS = 1,
                S = 2,
                A = 3,
                B = 4,
            }

            public override int Key => Id;
            public int Id;
            public int SkillId;
            public BuffRank Rank;
            public decimal Ratio;

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                Rank = (BuffRank) Enum.Parse(typeof(BuffRank), fields[1]);
                SkillId = ParseInt(fields[2]);
                Ratio = ParseDecimal(fields[3]);
            }
        }

        public CrystalRandomBuffSheet() : base(nameof(CrystalRandomBuffSheet))
        {
        }
    }
}
