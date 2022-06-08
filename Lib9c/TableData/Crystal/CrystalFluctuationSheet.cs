using System;
using System.Collections.Generic;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Crystal
{
    public class CrystalFluctuationSheet : Sheet<int, CrystalFluctuationSheet.Row>
    {
        public enum ServiceType
        {
            Combination,
        }

        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id;
            public ServiceType Type;
            public long BlockInterval;
            public int MinimumRate;
            public int MaximumRate;
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                Type = (ServiceType) Enum.Parse(typeof(ServiceType), fields[1]);
                BlockInterval = ParseLong(fields[2]);
                MinimumRate = ParseInt(fields[3]);
                MaximumRate = ParseInt(fields[4]);
            }
        }

        public CrystalFluctuationSheet() : base(nameof(CrystalFluctuationSheet))
        {
        }
    }
}
