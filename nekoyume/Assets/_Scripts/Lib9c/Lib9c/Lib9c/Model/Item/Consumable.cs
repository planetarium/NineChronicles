using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Consumable : ItemUsable
    {
        public StatType MainStat => Stats.Any() ? Stats[0].StatType : StatType.NONE;

        public List<StatMap> Stats { get; }

        public Consumable(ConsumableItemSheet.Row data, Guid id, long requiredBlockIndex) : base(data, id, requiredBlockIndex)
        {
            Stats = data.Stats;
        }

        public Consumable(Dictionary serialized) : base(serialized)
        {
            if (serialized.TryGetValue((Text) "stats", out var stats))
            {
                Stats = stats.ToList(i => new StatMap((Dictionary) i));
            }
        }
        
        protected Consumable(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "stats"] = new List(Stats
                    .OrderBy(i => i.StatType)
                    .ThenByDescending(i => i.Value)
                    .Select(s => s.Serialize())),
            }.Union((Dictionary) base.Serialize()));
#pragma warning restore LAA1002
    }
}
