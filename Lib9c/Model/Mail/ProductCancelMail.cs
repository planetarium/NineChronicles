using System;
using Bencodex.Types;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class ProductCancelMail : Mail
    {
        public readonly Guid ProductId;
        public ProductCancelMail(long blockIndex, Guid id, long requiredBlockIndex, Guid productId) : base(blockIndex, id, requiredBlockIndex)
        {
            ProductId = productId;
        }

        public ProductCancelMail(Dictionary serialized) : base(serialized)
        {
            ProductId = serialized[ProductIdKey].ToGuid();
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        protected override string TypeId => nameof(ProductCancelMail);

        public override IValue Serialize() => ((Dictionary)base.Serialize())
            .Add(ProductIdKey, ProductId.Serialize());
    }
}
