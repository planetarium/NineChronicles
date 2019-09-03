using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public abstract class AttachmentMail : Mail
    {
        public ActionResult attachment;

        protected AttachmentMail(ActionResult actionResult, long blockIndex) : base(actionResult, blockIndex)
        {
            attachment = actionResult;
        }
    }
}
