using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.Action;
using Nekoyume.State;
using Nekoyume.TableData;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class SellerMail : AttachmentMail
    {
        protected override string TypeId => "seller";
        public override MailType MailType => MailType.Auction;

        public SellerMail(AttachmentActionResult attachmentActionResult, long blockIndex) : base(attachmentActionResult,
            blockIndex)
        {
        }

        public SellerMail(Bencodex.Types.Dictionary serialized) : base(serialized)
        {
        }

        public override string ToInfo()
        {
            if (!(attachment is Buy.SellerResult sellerResult))
                throw new InvalidCastException($"({nameof(Buy.SellerResult)}){nameof(attachment)}");
            
            var format = LocalizationManager.Localize("UI_SELLER_MAIL_FORMAT");
            return string.Format(format, sellerResult.gold, sellerResult.itemUsable.Data.GetLocalizedName());
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
