using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class SellCancelMail : AttachmentMail
    {
        public SellCancelMail(ActionResult actionResult, long blockIndex) : base(actionResult, blockIndex)
        {
            attachment = (SellCancellation.Result) actionResult;
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
