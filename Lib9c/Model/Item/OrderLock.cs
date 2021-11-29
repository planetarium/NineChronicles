using System;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public struct OrderLock : ILock
    {
        public LockType Type => LockType.Order;
        public readonly Guid OrderId;

        public OrderLock(Guid orderId)
        {
            OrderId = orderId;
        }

        public OrderLock(List serialized)
        {
            OrderId = serialized[1].ToGuid();
        }

        public IValue Serialize() => new List(
            Type.Serialize(),
            OrderId.Serialize()
        );
    }
}
