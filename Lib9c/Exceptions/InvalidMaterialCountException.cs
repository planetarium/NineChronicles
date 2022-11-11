using System;

namespace Nekoyume.Exceptions
{
    [Serializable]
    public class InvalidMaterialCountException : Exception
    {
        public InvalidMaterialCountException(
            string actionType,
            string addressesHex,
            int required,
            int playerHas)
            : this($"[{actionType}][{addressesHex}] Aborted as the player has no enough material. " +
                   $"It required {required}, But Player Has {playerHas}.")
        {
        }

        public InvalidMaterialCountException(string message) : base(message)
        {
        }
    }
}
