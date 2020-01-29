using System;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.TableData;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class SellCancelMail : AttachmentMail
    {
        protected override string TypeId => "sellCancel";
        public override MailType MailType => MailType.Auction;

        public SellCancelMail(SellCancellation.Result attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {

        }

        public SellCancelMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
        }

        public override string ToInfo()
        {
            var format = LocalizationManager.Localize("UI_SELL_CANCEL_MAIL_FORMAT");
            return string.Format(format, attachment.itemUsable.Data.GetLocalizedName());
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
