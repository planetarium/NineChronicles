using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class SellCancelMail : AttachmentMail
    {
        public override MailType MailType { get => MailType.Auction; }

        public SellCancelMail(SellCancellation.Result attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {
            
        }

        public override string ToInfo()
        {
            return "판매 취소 완료";
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
