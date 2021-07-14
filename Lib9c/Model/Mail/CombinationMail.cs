using System;
using Nekoyume.Action;

namespace Nekoyume.Model.Mail
{
    // todo: `CombineConsumable`, `CombineEquipment`, `EnhanceEquipment`로 분리할 필요가 있어 보임(소모품을 n개 만들었을 때 재료가 n개 씩 노출됨)
    [Serializable]
    public class CombinationMail : AttachmentMail
    {
        protected override string TypeId => "combinationMail";
        public override MailType MailType => MailType.Workshop;

        public CombinationMail(CombinationConsumable5.ResultModel attachmentActionResult, long blockIndex, Guid id, long requiredBlockIndex)
            : base(attachmentActionResult, blockIndex, id, requiredBlockIndex)
        {

        }

        public CombinationMail(Bencodex.Types.Dictionary serialized)
            : base(serialized)
        {
        }

        public override void Read(IMail mail)
        {
            mail.Read(this);
        }
    }
}
