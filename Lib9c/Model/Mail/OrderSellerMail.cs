using System;
using Bencodex.Types;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class OrderSellerMail : Mail
    {
        public readonly Guid OrderId;
        public OrderSellerMail(long blockIndex, Guid id, long requiredBlockIndex, Guid orderId) : base(blockIndex, id, requiredBlockIndex)
        {
            OrderId = orderId;
        }

        public OrderSellerMail(Dictionary serialized) : base(serialized)
        {
            OrderId = serialized[OrderIdKey].ToGuid();
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public override MailType MailType => MailType.Auction;

        protected override string TypeId => nameof(OrderSellerMail);

        public override IValue Serialize() => ((Dictionary)base.Serialize())
            .Add(OrderIdKey, OrderId.Serialize());
    }
}
