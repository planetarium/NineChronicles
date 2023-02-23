using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoyume.TableData.Pet
{
    using static TableExtensions;

    [Serializable]
    public class PetCostSheet : Sheet<int, PetCostSheet.Row>
    {
        [Serializable]
        public class PetCostData
        {
            public int Level { get; }
            public int SoulStoneQuantity { get; }
            public int NcgQuantity { get; }

            public PetCostData(
                int level,
                int soulStoneQuantity,
                int ncgQuantity)
            {
                Level = level;
                SoulStoneQuantity = soulStoneQuantity;
                NcgQuantity = ncgQuantity;
            }
        }

        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => PetId;

            public int PetId { get; private set; }

            public List<PetCostData> Cost { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                PetId = ParseInt(fields[0]);
                var level = ParseInt(fields[1]);
                var soulStone = ParseInt(fields[2]);
                var ncg = ParseInt(fields[3]);
                Cost = new List<PetCostData>
                {
                   new PetCostData(level, soulStone, ncg)
                };
            }

            public bool TryGetCost(int level, out PetCostData costData)
            {
                costData = Cost.FirstOrDefault(x => x.Level.Equals(level));
                return !(costData is null);
            }
        }

        public PetCostSheet() : base(nameof(PetCostSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (!value.Cost.Any())
            {
                return;
            }

            row.Cost.Add(value.Cost[0]);
        }
    }
}

