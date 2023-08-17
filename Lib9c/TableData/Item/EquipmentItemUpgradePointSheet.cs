using System;
using System.Collections.Generic;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using static Nekoyume.TableData.TableExtensions;

namespace Nekoyume.TableData
{
    [Serializable]
    public class EquipmentItemUpgradePointSheet : Sheet<int, EquipmentItemUpgradePointSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public ItemSubType ItemSubType { get; private set; }
            public int Level { get; private set; }
            public Dictionary<Grade, long> ExpMap { get; private set; }

            public Row()
            {
            }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = ParseInt(fields[0]);
                ItemSubType = (ItemSubType)Enum.Parse(typeof(ItemSubType), fields[1]);
                Level = ParseInt(fields[2]);
                ExpMap = new Dictionary<Grade, long>
                {
                    { Grade.Normal, ParseLong(fields[3]) },
                    { Grade.Rare, ParseLong(fields[4]) },
                    { Grade.Epic, ParseLong(fields[5]) },
                    { Grade.Unique, ParseLong(fields[6]) },
                    { Grade.Legendary, ParseLong(fields[7]) },
                };
            }
        }

        public EquipmentItemUpgradePointSheet() : base(nameof(EquipmentItemUpgradePointSheet))
        {
        }
    }
}
