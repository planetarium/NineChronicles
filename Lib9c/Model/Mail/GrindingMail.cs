using System;
using Bencodex.Types;
using Libplanet.Types.Assets;
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
        public override IValue Serialize() => ((Dictionary)base.Serialize())
            .Add("ic", ItemCount.Serialize())
            .Add("a", Asset.Serialize());
    }
}
