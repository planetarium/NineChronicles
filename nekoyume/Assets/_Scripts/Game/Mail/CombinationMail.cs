using System;
using Nekoyume.Action;

namespace Nekoyume.Game.Mail
{
    [Serializable]
    public class CombinationMail : Mail
    {
        public CombinationMail(ActionResult actionResult, long blockIndex) : base(actionResult, blockIndex)
        {
        }

        public override string ToInfo()
        {
            return "조합 완료";
        }
    }
}
