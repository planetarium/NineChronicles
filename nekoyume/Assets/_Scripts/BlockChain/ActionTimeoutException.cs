using System;

namespace Nekoyume.BlockChain
{
    public class ActionTimeoutException : TimeoutException
    {
        public readonly Guid ActionId;

        public ActionTimeoutException(string message, Guid actionId) : base(message)
        {
            ActionId = actionId;
        }
    }
}
