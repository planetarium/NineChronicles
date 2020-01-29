using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Action;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public abstract class AttachmentMail : Mail
    {
        public AttachmentActionResult attachment;
        public string AttachmentName => attachment.itemUsable.GetLocalizedName();

        protected AttachmentMail(AttachmentActionResult attachmentActionResult, long blockIndex) : base(blockIndex)
        {
            attachment = attachmentActionResult;
        }

        public AttachmentMail(Dictionary serialized)
            : base(serialized)
        {
            attachment = AttachmentActionResult.Deserialize(
                (Dictionary)serialized["attachment"]
            );
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"attachment"] = attachment.Serialize(),
            }.Union((Dictionary)base.Serialize()));
    }
}
