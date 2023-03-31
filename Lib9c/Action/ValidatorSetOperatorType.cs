namespace Nekoyume.Action
{
    public enum ValidatorSetOperatorType
    {
        /// <summary>
        /// Append the validator. no-op if validator public key already exists.
        /// </summary>
        Append,

        /// <summary>
        /// Set validator's power to 0.
        /// </summary>
        Remove,

        /// <summary>
        /// Update validator. if not exists, append it.
        /// </summary>
        Update,
    }
}
