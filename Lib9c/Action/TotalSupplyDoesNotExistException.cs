using Libplanet;
using Libplanet.Assets;
using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class TotalSupplyDoesNotExistException : Exception
    {
        public TotalSupplyDoesNotExistException(Currency currency)
        {
            Currency = currency;
        }

        protected TotalSupplyDoesNotExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Currency = (Currency) info.GetValue(nameof(Currency), typeof(Currency));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Currency), Currency);
        }

        public Currency Currency { get; }
    }
}
