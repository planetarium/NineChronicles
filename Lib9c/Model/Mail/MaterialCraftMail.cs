using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.Item;
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
        public override IValue Serialize() =>
#pragma warning disable LAA1002
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"ic"] = ItemCount.Serialize(),
                [(Text)"iid"] = ItemId.Serialize(),
            }.Union((Dictionary)base.Serialize()));
#pragma warning restore LAA1002

    }
}
