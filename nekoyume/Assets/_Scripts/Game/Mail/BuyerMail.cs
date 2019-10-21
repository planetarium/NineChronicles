using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class BuyerMail : AttachmentMail
    {
        protected override string TypeId => "buyerMail";

        public BuyerMail(AttachmentActionResult attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {
        }

        public BuyerMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
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
