using System;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.TableData;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class BuyerMail : AttachmentMail
    {
        protected override string TypeId => "buyerMail";
        public override MailType MailType => MailType.Auction;

        public BuyerMail(AttachmentActionResult attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {

        }

        public BuyerMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
        }

        public override string ToInfo()
        {
            var format = LocalizationManager.Localize("UI_BUYER_MAIL_FORMAT");
            return string.Format(format, attachment.itemUsable.Data.GetLocalizedName());
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
