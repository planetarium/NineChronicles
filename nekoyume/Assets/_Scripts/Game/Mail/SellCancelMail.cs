using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class SellCancelMail : AttachmentMail
    {
        protected override string TypeId => "sellCancel";
        public override MailType MailType { get => MailType.Auction; }

        public SellCancelMail(SellCancellation.Result attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {
            
        }

        public SellCancelMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
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
