using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class CombinationMail : AttachmentMail
    {
        public CombinationMail(Combination.Result attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {
        }

        public override string ToInfo()
        {
            return "조합 완료";
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

    }
}
