using System;
using Nekoyume.Model.Mail;

namespace Nekoyume.UI.Model
{
    public class RaidRewardMail : Mail
    {
        public readonly SeasonRewardRecord SeasonRewardRecord;

        public RaidRewardMail(long blockIndex, Guid id, long requiredBlockIndex, SeasonRewardRecord seasonRewardRecord)
            : base(blockIndex, id, requiredBlockIndex)
        {
            SeasonRewardRecord = seasonRewardRecord;
        }

        public override void Read(IMail mail)
        {
            if (mail is MailPopup mailPopup)
            {
                mailPopup.Read(this);
            }
        }

        public override MailType MailType => MailType.System;

        protected override string TypeId => nameof(RaidRewardMail);
#pragma warning restore LAA1002
    }
}

