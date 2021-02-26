using System;
using Bencodex.Types;
using Nekoyume.Action;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class SellerMail : AttachmentMail
    {
        protected override string TypeId => "seller";
        public override MailType MailType => MailType.Auction;

        public SellerMail(AttachmentActionResult attachmentActionResult, long blockIndex, Guid id, long requiredBlockIndex) : base(attachmentActionResult,
            blockIndex, id, requiredBlockIndex)
        {
        }

        public SellerMail(Dictionary serialized) : base(serialized)
        {
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
