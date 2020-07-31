using System;
using Nekoyume.Action;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class BuyerMail : AttachmentMail
    {
        protected override string TypeId => "buyerMail";
        public override MailType MailType => MailType.Auction;

        public BuyerMail(AttachmentActionResult attachmentActionResult, long blockIndex, Guid id, long requiredBlockIndex)
            : base(attachmentActionResult, blockIndex, id, requiredBlockIndex)
        {

        }

        public BuyerMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
