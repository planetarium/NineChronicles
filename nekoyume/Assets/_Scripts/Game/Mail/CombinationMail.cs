using System;
using Assets.SimpleLocalization;
using Nekoyume.Action;
using Nekoyume.Game.Item;
using Nekoyume.TableData;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class CombinationMail : AttachmentMail
    {
        private static readonly string _format = LocalizationManager.Localize("UI_COMBINATION_NOTIFY_FORMAT");
        protected override string TypeId => "combinationMail";
        public override MailType MailType => MailType.Forge;

        public CombinationMail(Combination.ResultModel attachmentActionResult, long blockIndex) : base(attachmentActionResult, blockIndex)
        {
            
        }

        public CombinationMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
        }

        public override string ToInfo()
        {   
            return string.Format(_format, AttachmentName);
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
