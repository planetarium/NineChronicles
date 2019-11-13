using System;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.TableData;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class SellerMail : AttachmentMail
    {
        protected override string TypeId => "seller";
        public override MailType MailType => MailType.Auction;

        public SellerMail(AttachmentActionResult attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {
            
        }

        public SellerMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
        }

        public override string ToInfo()
        {
            var format = LocalizationManager.Localize("UI_SELLER_MAIL_FORMAT");
            return string.Format(format, attachment.itemUsable.Data.GetLocalizedName());
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
