using System;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class MaterialCraftMail : Mail
    {
        public int ItemCount;
        public int ItemId;

        public MaterialCraftMail(
            long blockIndex,
            Guid id,
            long requiredBlockIndex,
            int itemCount,
            int itemId
        ) : base(blockIndex, id, requiredBlockIndex)
        {
            ItemCount = itemCount;
            ItemId = itemId;
        }

        public MaterialCraftMail(Dictionary serialized) : base(serialized)
        {
            ItemCount = serialized["ic"].ToInteger();
            ItemId = serialized["iid"].ToInteger();
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public override MailType MailType => MailType.Workshop;

        protected override string TypeId => nameof(MaterialCraftMail);
        public override IValue Serialize() => ((Dictionary)base.Serialize())
            .Add("ic", ItemCount.Serialize())
            .Add("iid", ItemId.Serialize());
    }
}
