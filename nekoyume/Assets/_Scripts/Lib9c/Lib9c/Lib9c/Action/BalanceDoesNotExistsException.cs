using Libplanet;
using Libplanet.Assets;
using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class BalanceDoesNotExistsException : Exception
    {
        public BalanceDoesNotExistsException(Address address, Currency currency)
        {
            Address = address;
            Currency = currency;
        }

        protected BalanceDoesNotExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Address = (Address) info.GetValue(nameof(Address), typeof(Address));
            Currency = (Currency) info.GetValue(nameof(Currency), typeof(Currency));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Address), Address);
            info.AddValue(nameof(Currency), Currency);
        }

        public Address Address { get; }
        public Currency Currency { get; }
    }
}
