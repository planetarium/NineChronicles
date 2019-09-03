using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class SellCancelMail : Mail
    {
        public SellCancelMail(ActionResult actionResult, long blockIndex) : base(actionResult, blockIndex)
        {
        }

        public override string ToInfo()
        {
            return "판매 취소 완료";
        }
    }
}
