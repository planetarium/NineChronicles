namespace Nekoyume.BlockChain.Policy
{
    public sealed class GenericSubPolicy<T> : VariableSubPolicy<T>
    {
        private GenericSubPolicy(T defaultValue)
            : base(defaultValue)
        {
        }

        private GenericSubPolicy(
            GenericSubPolicy<T> genericPolicy,
            SpannedSubPolicy<T> spannedSubPolicy)
            : base(genericPolicy, spannedSubPolicy)
        {
        }

        public static GenericSubPolicy<T> Create(T defaultValue) =>
            new GenericSubPolicy<T>(defaultValue);
    }
}
