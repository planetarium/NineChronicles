using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Assets;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class GoldCurrencyState : State, ISerializable
    {
        public static readonly Address Address = Addresses.GoldCurrency;

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
                [(Text)"currency"] = CurrencyExtensions.Serialize(Currency)
            };
#pragma warning disable LAA1002
            return new Dictionary(values.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", new Codec().Encode(Serialize()));
        }
    }
}
