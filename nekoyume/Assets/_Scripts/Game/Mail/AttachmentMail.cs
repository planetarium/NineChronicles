using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Action;
using Nekoyume.Game.Item;
using Nekoyume.TableData;

namespace Nekoyume.Game.Mail
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

        public AttachmentMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
            attachment = AttachmentActionResult.Deserialize(
                (Bencodex.Types.Dictionary) serialized[(Text) "attachment"]
            );
        }

        public override IValue Serialize() =>
            new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "attachment"] = attachment.Serialize(),
            }.Union((Bencodex.Types.Dictionary) base.Serialize()));
    }
}
