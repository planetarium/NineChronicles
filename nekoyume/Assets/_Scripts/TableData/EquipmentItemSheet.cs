using System;
using System.Collections.Generic;
using Nekoyume.EnumType;

namespace Nekoyume.TableData
{
    [Serializable]
    public class EquipmentItemSheet : Sheet<int, EquipmentItemSheet.Row>
    {
        [Serializable]
        public class Row : ConsumableItemSheet.Row
        {
            public override ItemType ItemType => ItemType.Equipment;
            public decimal AttackRange { get; private set; }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                AttackRange = decimal.Parse(fields[9]);
            }
        }
        
        public EquipmentItemSheet() : base(nameof(EquipmentItemSheet))
        {
        }

        protected override void AddRow(int key, Row value)
        {
            if (!TryGetValue(key, out var row))
            {
                Add(key, value);

                return;
            }

            if (value.Stats.Count == 0)
                return;

            row.Stats.Add(value.Stats[0]);
        }
    }
}
