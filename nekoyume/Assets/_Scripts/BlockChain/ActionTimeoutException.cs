using System;
using System.Runtime.Serialization;
using Libplanet.Tx;

namespace Nekoyume.BlockChain
{
    public class ActionTimeoutException : TimeoutException
    {
        public readonly TxId? TxId;
        public readonly Guid ActionId;

        public ActionTimeoutException(string message, TxId? txId, Guid actionId) : base(message)
        {
            TxId = txId;
            ActionId = actionId;
        }

        protected ActionTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ActionId = (Guid) info.GetValue(nameof(ActionId), typeof(Guid));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(ActionId), ActionId);
        }
    }
}
