using System;
using Nekoyume.Action;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class DailyRewardMail : AttachmentMail
    {
        protected override string TypeId => "dailyRewardMail";
        public override MailType MailType => MailType.System;

        public DailyRewardMail(AttachmentActionResult attachmentActionResult, long blockIndex, Guid id, long requiredBlockIndex)
            : base(attachmentActionResult, blockIndex, id, requiredBlockIndex)
        {

        }

        public DailyRewardMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
