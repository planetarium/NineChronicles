using System;
using Nekoyume.Action;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class SellCancelMail : AttachmentMail
    {
        protected override string TypeId => "sellCancel";
        public override MailType MailType => MailType.Auction;

        public SellCancelMail(SellCancellation.Result attachmentActionResult, long blockIndex, Guid id, long requiredBlockIndex)
            : base(attachmentActionResult, blockIndex, id, requiredBlockIndex)
        {

        }

        public SellCancelMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
