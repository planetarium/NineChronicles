using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class SellCancelMail : AttachmentMail
    {
        public SellCancelMail(AttachmentActionResult attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {
            attachment = (SellCancellation.Result) attachmentActionResult;
        }

        public override string ToInfo()
        {
            return "판매 취소 완료";
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        public SellCancellation.Result GetAttachment()
        {
            return (SellCancellation.Result) attachment;
        }
    }
}
