using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Libplanet;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class GoldCurrencyState : State, ISerializable
    {
        public static readonly Address Address = new Address(
            new byte[]
            {
                0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0xA,
            }
        );

        public Currency Currency { get; private set; }

        public GoldCurrencyState(Currency currency)
            : base(Address)
        {
            Currency = currency;
        }

        public GoldCurrencyState(Dictionary serialized)
            : base(serialized)
        {
            Currency = CurrencyExtensions.Deserialize((Dictionary) serialized["currency"]);
        }

        protected GoldCurrencyState(SerializationInfo info, StreamingContext context)
            : this((Dictionary) new Codec().Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public override IValue Serialize()
        {
            var values = new Dictionary<IKey, IValue>
            {
                [(Text)"currency"] = Currency.Serialize()
            };
            return new Dictionary(values.Union((Dictionary)base.Serialize()));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", new Codec().Encode(Serialize()));
        }
    }
}
