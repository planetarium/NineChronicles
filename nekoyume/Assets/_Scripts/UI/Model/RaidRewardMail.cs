using System;
using Nekoyume.Model.Mail;

namespace Nekoyume.UI.Model
{
    public class RaidRewardMail : Mail
    {
        public readonly string CurrencyName;
        public readonly long Amount;
        public readonly int RaidId;

        public RaidRewardMail(long blockIndex, Guid id, long requiredBlockIndex, string currencyName, long amount, int raidId)
            : base(blockIndex, id, requiredBlockIndex)
        {
            CurrencyName = currencyName;
            Amount = amount;
            RaidId = raidId;
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

