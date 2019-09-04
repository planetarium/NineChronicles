using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class CombinationMail : AttachmentMail
    {
        public CombinationMail(AttachmentActionResult attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {
            attachment = (Combination.Result) attachmentActionResult;
        }

        public override string ToInfo()
        {
            return "조합 완료";
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public Combination.Result GetAttachment()
        {
            return (Combination.Result) attachment;
        }
    }
}
