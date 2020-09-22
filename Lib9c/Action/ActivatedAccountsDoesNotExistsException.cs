using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class ActivatedAccountsDoesNotExistsException : ActivationException
    {
        public ActivatedAccountsDoesNotExistsException()
        {
        }

        public ActivatedAccountsDoesNotExistsException(
            SerializationInfo info, StreamingContext context
        ) : base(info, context)
        {
        }
    }
}
