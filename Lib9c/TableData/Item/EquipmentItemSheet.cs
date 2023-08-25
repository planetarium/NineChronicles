using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using static Nekoyume.TableData.TableExtensions;

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
            public long? Exp { get; private set; }

            public Row()
            {
            }

            public Row(Dictionary serialized) : base(serialized)
            {
                SetId = (Integer)serialized["set_id"];
                Stat = serialized["stat"].ToDecimalStat();
                AttackRange = serialized["attack_range"].ToDecimal();
                SpineResourcePath = (Text)serialized["spine_resource_path"];
                if (serialized.ContainsKey("exp"))
                {
                    Exp = serialized["exp"].ToLong();
                }
            }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                SetId = string.IsNullOrEmpty(fields[4]) ? 0 : ParseInt(fields[4]);
                Stat = new DecimalStat(
                    (StatType)Enum.Parse(typeof(StatType), fields[5]),
                    ParseDecimal(fields[6]));
                AttackRange = ParseDecimal(fields[7]);
                SpineResourcePath = fields[8];
                if (fields.Count >= 10)
                {
                    Exp = string.IsNullOrEmpty(fields[9]) ? 0L : ParseLong(fields[9]);
                }
            }

#pragma warning disable LAA1002
            public override IValue Serialize()
            {
                var pairs = new Dictionary<IKey, IValue>
                {
                    [(Text)"set_id"] = (Integer)SetId,
                    [(Text)"stat"] = Stat.SerializeForLegacyEquipmentStat(),
                    [(Text)"attack_range"] = AttackRange.Serialize(),
                    [(Text)"spine_resource_path"] = (Text)SpineResourcePath,
                };
                if (!(Exp is null))
                {
                    pairs[(Text)"exp"] = Exp.Serialize();
                }

                return new Dictionary(
                    new Dictionary<IKey, IValue>(pairs).Union((Dictionary)base.Serialize())
                );
            }
#pragma warning restore LAA1002
        }

        public EquipmentItemSheet() : base(nameof(EquipmentItemSheet))
        {
        }
    }
}
