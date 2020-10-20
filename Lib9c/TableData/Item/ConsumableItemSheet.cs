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
    public class ConsumableItemSheet : Sheet<int, ConsumableItemSheet.Row>
    {
        [Serializable]
        public class Row : ItemSheet.Row, IState
        {
            public override ItemType ItemType => ItemType.Consumable;
            public List<StatMap> Stats { get; private set; }
            
            public Row() {}

            public Row(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                Stats = ((Bencodex.Types.List) serialized["stats"]).Select(value =>
                    new StatMap((Bencodex.Types.Dictionary) value)).ToList();
            }

            public override void Set(IReadOnlyList<string> fields)
            {
                base.Set(fields);
                Stats = new List<StatMap>();
                for (var i = 0; i < 2; i++)
                {
                    if (string.IsNullOrEmpty(fields[4 + i * 2]) ||
                        string.IsNullOrEmpty(fields[5 + i * 2]))
                        return;

                    Stats.Add(new StatMap(
                        (StatType) Enum.Parse(typeof(StatType), fields[4 + i * 2]),
                        ParseDecimal(fields[5 + i * 2])));
                }
            }

            public override IValue Serialize() =>
#pragma warning disable LAA1002
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "stats"] = new Bencodex.Types.List(Stats.Select(stat => stat.Serialize())),
                }.Union((Bencodex.Types.Dictionary) base.Serialize()));
#pragma warning restore LAA1002

            public new static Row Deserialize(Bencodex.Types.Dictionary serialized)
            {
                return new Row(serialized);
            }
        }
        
        public ConsumableItemSheet() : base(nameof(ConsumableItemSheet))
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
