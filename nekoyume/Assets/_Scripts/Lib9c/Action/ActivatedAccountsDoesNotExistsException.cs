using System;

namespace Nekoyume.Action
{
    [Serializable]
    public class ActivatedAccountsDoesNotExistsException : ActivationException
    {
        public ActivatedAccountsDoesNotExistsException()
        {
        }
    }
}
