using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class ItemEnhanceMail : AttachmentMail
    {
        public override MailType MailType { get => MailType.Forge; }

        public ItemEnhanceMail(AttachmentActionResult attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
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
