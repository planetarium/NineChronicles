using System.Collections.Generic;
using System.Linq;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    public class RuneWeightSheet : Sheet<int, RuneWeightSheet.Row>
    {
        public class RuneInfo
        {
            public int RuneId;
            public decimal Weight;

            public RuneInfo(int runeId, decimal weight)
            {
                RuneId = runeId;
                Weight = weight;
            }
        }
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id;
            public int BossId;
            public int Rank;
            public List<RuneInfo> RuneInfos;
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                BossId = ParseInt(fields[1]);
                Rank = ParseInt(fields[2]);
                RuneInfos = new List<RuneInfo>
                {
                    new RuneInfo(ParseInt(fields[3]), ParseDecimal(fields[4]))

                };
            }
        }

        public RuneWeightSheet() : base(nameof(RuneWeightSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (!value.RuneInfos.Any())
            {
                return;
            }

            row.RuneInfos.Add(value.RuneInfos[0]);
        }
    }
}
