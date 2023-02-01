using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Pet;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData.Pet
{
    [Serializable]
    public class PetOptionSheet : Sheet<int, PetOptionSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public class PetOptionInfo
            {
                public PetOptionType OptionType { get; }
                public decimal OptionValue { get; }

                public PetOptionInfo(
                    PetOptionType optionType,
                    decimal optionValue)
                {
                    OptionType = optionType;
                    OptionValue = optionValue;
                }
            }

            public override int Key => PetId;
            public int PetId { get; private set; }
            public Dictionary<int, PetOptionInfo> LevelOptionMap { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                LevelOptionMap = new Dictionary<int, PetOptionInfo>();

                PetId = ParseInt(fields[0]);
                var level = ParseInt(fields[1]);
                var petOptionType = (PetOptionType)Enum.Parse(typeof(PetOptionType), fields[2]);
                var optionValue = ParseDecimal(fields[3]);
                LevelOptionMap[level] = new PetOptionInfo(petOptionType, optionValue);
            }
        }

        public PetOptionSheet() : base(nameof(PetOptionSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (value.LevelOptionMap.Count == 0)
                return;

            var pair = value.LevelOptionMap.OrderBy(x => x.Key).First();
            row.LevelOptionMap[pair.Key] = pair.Value;
        }
    }
}
