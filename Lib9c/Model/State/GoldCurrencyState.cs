using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Model.State
{
    [Serializable]
    public class GoldCurrencyState : State, ISerializable
    {
        public const long DEFAULT_INITIAL_SUPPLY = 1000000000;

        public static readonly Address Address = Addresses.GoldCurrency;

        public Currency Currency { get; private set; }

        public long InitialSupply { get; private set; }

        public GoldCurrencyState(Currency currency)
            : this(currency, DEFAULT_INITIAL_SUPPLY)
        {
        }

        public GoldCurrencyState(Currency currency, long initialSupply)
            : base(Address)
        {
            Currency = currency;
            InitialSupply = initialSupply;
        }

        public GoldCurrencyState(Dictionary serialized)
            : base(serialized)
        {
            Currency = CurrencyExtensions.Deserialize((Dictionary) serialized["currency"]);
            if (serialized.TryGetValue((Text)"initialSupply", out IValue rawInitialSupply))
            {
                InitialSupply = (Integer)rawInitialSupply;
            }
            else
            {
                InitialSupply = DEFAULT_INITIAL_SUPPLY;
            }
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

            if (InitialSupply != DEFAULT_INITIAL_SUPPLY)
            {
                values.Add((Text)"initialSupply", (Integer)InitialSupply);
            }
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
