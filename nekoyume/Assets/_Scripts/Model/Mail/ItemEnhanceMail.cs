using System;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.TableData;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class ItemEnhanceMail : AttachmentMail
    {
        protected override string TypeId => "itemEnhance";
        public override MailType MailType => MailType.Workshop;

        public ItemEnhanceMail(AttachmentActionResult attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {

        }

        public ItemEnhanceMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
        }

        public override string ToInfo()
        {
            var format = LocalizationManager.Localize("UI_ITEM_ENHANCEMENT_MAIL_FORMAT");
            return string.Format(format, attachment.itemUsable.Data.GetLocalizedName());
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
