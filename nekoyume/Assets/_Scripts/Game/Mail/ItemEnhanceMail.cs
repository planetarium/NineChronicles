using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
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
            return "아이템 강화 완료";
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
