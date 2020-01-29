using System;
using Assets.SimpleLocalization;
using Nekoyume.Action;

namespace Nekoyume.Model.Mail
{
    // todo: `CombineConsumable`, `CombineEquipment`, `EnhanceEquipment`로 분리할 필요가 있어 보임(소모품을 n개 만들었을 때 재료가 n개 씩 노출됨)
    [Serializable]
    public class CombinationMail : AttachmentMail
    {
        private static readonly string _format = LocalizationManager.Localize("UI_COMBINATION_NOTIFY_FORMAT");
        protected override string TypeId => "combinationMail";
        public override MailType MailType => MailType.Workshop;

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
