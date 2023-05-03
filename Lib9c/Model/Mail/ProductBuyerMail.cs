using System;
using Bencodex.Types;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;


namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class ProductBuyerMail : Mail
    {
        public readonly Guid ProductId;
        public ProductBuyerMail(long blockIndex, Guid id, long requiredBlockIndex, Guid productId) : base(blockIndex, id, requiredBlockIndex)
        {
            ProductId = productId;
        }

        public ProductBuyerMail(Dictionary serialized) : base(serialized)
        {
            ProductId = serialized[ProductIdKey].ToGuid();
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public override MailType MailType => MailType.Auction;

        protected override string TypeId => nameof(ProductBuyerMail);

        public override IValue Serialize() => ((Dictionary)base.Serialize())
            .Add(ProductIdKey, ProductId.Serialize());
    }
}
