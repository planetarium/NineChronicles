using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public abstract class AttachmentMail : Mail
    {
        public AttachmentActionResult attachment;

        protected AttachmentMail(AttachmentActionResult attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {
            attachment = attachmentActionResult;
        }
    }
}
