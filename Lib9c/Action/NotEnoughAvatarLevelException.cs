using System;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    public class NotEnoughAvatarLevelException : Exception
    {
        public NotEnoughAvatarLevelException(string message) : base(message)
        {
        }

        public NotEnoughAvatarLevelException(int require, int current)
            : this($"Aborted as the signer is not enough the minimum avatar level required: {current} < {require}.")
        {
        }

        public NotEnoughAvatarLevelException(int itemId, bool isMadeWithMimisbrunnrRecipe, int require, int current)
            : this($"Aborted as the signer is not enough the minimum avatar level required: {nameof(itemId)}({itemId}), {nameof(isMadeWithMimisbrunnrRecipe)}({isMadeWithMimisbrunnrRecipe}), {current} < {require}.")
        {
        }

        public NotEnoughAvatarLevelException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
