using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Assets;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Mail
{
    public class GrindingMail : Mail
    {
        public readonly int ItemCount;
        public FungibleAssetValue Asset;
        public GrindingMail(long blockIndex, Guid id, long requiredBlockIndex, int itemCount, FungibleAssetValue asset) : base(blockIndex, id, requiredBlockIndex)
        {
            ItemCount = itemCount;
            Asset = asset;
        }

        public GrindingMail(Dictionary serialized) : base(serialized)
        {
            ItemCount = serialized["ic"].ToInteger();
            Asset = serialized["a"].ToFungibleAssetValue();
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public override MailType MailType => MailType.Grinding;

        protected override string TypeId => nameof(GrindingMail);
        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"ic"] = ItemCount.Serialize(),
                [(Text)"a"] = Asset.Serialize(),
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002

    }
}
