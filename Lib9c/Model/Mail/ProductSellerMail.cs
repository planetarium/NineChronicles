using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class ProductSellerMail : Mail
    {
        public readonly Guid ProductId;
        public ProductSellerMail(long blockIndex, Guid id, long requiredBlockIndex, Guid productId) : base(blockIndex, id, requiredBlockIndex)
        {
            ProductId = productId;
        }

        public ProductSellerMail(Dictionary serialized) : base(serialized)
        {
            ProductId = serialized[ProductIdKey].ToGuid();
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public override MailType MailType => MailType.Auction;

        protected override string TypeId => nameof(ProductSellerMail);

        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)ProductIdKey] = ProductId.Serialize(),
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002

    }
}
