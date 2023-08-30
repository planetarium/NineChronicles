using System;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Mail
{
    [Serializable]
    public class SummonMail : Mail
    {
        protected override string TypeId => nameof(SummonMail);
        public override MailType MailType => MailType.Summon;

        public SummonMail(long blockIndex, Guid id, long requiredBlockIndex) :
            base(blockIndex, id, requiredBlockIndex)
        {
            // TODO: Check what should be included in the summon mail
        }

        public SummonMail(Dictionary serialized) : base(serialized)
        {
            // TODO: Check what should be included in the summon mail
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }

        // TODO: Check what should be included in the summon mail
        public override IValue Serialize() => ((Dictionary)base.Serialize())
            .Add("key", 0.Serialize());
    }
}
