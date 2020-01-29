using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Bencodex.Types;
using Nekoyume.EnumType;
using Nekoyume.Model.Stat;
using Nekoyume.State;

namespace Nekoyume.TableData
{
    [Serializable]
    public class EquipmentItemSheet : Sheet<int, EquipmentItemSheet.Row>
    {
        [Serializable]
        public class Row : ItemSheet.Row
        {
            public override ItemType ItemType => ItemType.Equipment;
            public int SetId { get; private set; }
            public DecimalStat Stat { get; private set; }
            public decimal AttackRange { get; private set; }
            public string SpineResourcePath { get; private set; }

            public Row() {}

            public Row(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                SetId = (Integer) serialized["set_id"];
                Stat = serialized["stat"].ToDecimalStat();
                AttackRange = serialized["attack_range"].ToDecimal();
                SpineResourcePath = (Text) serialized["spine_resource_path"];
            }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                SetId = string.IsNullOrEmpty(fields[4]) ? 0 : int.Parse(fields[4]);
                Stat = new DecimalStat(
                    (StatType) Enum.Parse(typeof(StatType), fields[5]),
                    decimal.Parse(fields[6]));
                AttackRange = decimal.Parse(fields[7]);
                SpineResourcePath = fields[8];
            }
            
            public override IValue Serialize() => new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "set_id"] = (Integer) SetId,
                [(Text) "stat"] = Stat.Serialize(),
                [(Text) "attack_range"] = AttackRange.Serialize(),
                [(Text) "spine_resource_path"] = (Text) SpineResourcePath,
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
        }
        
        public EquipmentItemSheet() : base(nameof(EquipmentItemSheet))
        {
        }
    }
}
