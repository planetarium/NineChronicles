using System;
using System.Runtime.Serialization;
using Libplanet.Types.Tx;

namespace Nekoyume.Blockchain
{
    public class ActionTimeoutException : TimeoutException
    {
        public readonly TxId? TxId;
        public readonly Guid? ActionId;

        public ActionTimeoutException(string message, TxId? txId, Guid? actionId) : base(message)
        {
            TxId = txId;
            ActionId = actionId;
        }

        protected ActionTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            TxId = (TxId?)info.GetValue(nameof(TxId), typeof(TxId));
            ActionId = (Guid?)info.GetValue(nameof(ActionId), typeof(Guid));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue(nameof(TxId), TxId);
            info.AddValue(nameof(ActionId), ActionId);
        }
    }
}
