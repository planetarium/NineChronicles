using System;
using Bencodex.Types;
using Nekoyume.Action;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class StakingMail : AttachmentMail
    {
        public StakingMail(AttachmentActionResult attachmentActionResult, long blockIndex, Guid id, long requiredBlockIndex)
            : base(attachmentActionResult, blockIndex, id, requiredBlockIndex)
        {
        }

        public StakingMail(Dictionary serialized) : base(serialized)
        {
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        protected override string TypeId => "stakingMail";
        public override MailType MailType => MailType.System;
    }
}
