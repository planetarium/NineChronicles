using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class BuyerMail : AttachmentMail
    {
        public override MailType MailType { get => MailType.Auction; }

        public BuyerMail(AttachmentActionResult attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {
            
        }

        public override string ToInfo()
        {
            return "구매 완료";
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
