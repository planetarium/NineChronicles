using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class SellerMail : AttachmentMail
    {
        public SellerMail(AttachmentActionResult attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {
        }

        public override string ToInfo()
        {
            return "판매 완료";
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
