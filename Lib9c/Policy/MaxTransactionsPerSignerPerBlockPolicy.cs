namespace Nekoyume.BlockChain.Policy
{
    public sealed class MaxTransactionsPerSignerPerBlockPolicy : VariableSubPolicy<int>
    {
        private MaxTransactionsPerSignerPerBlockPolicy(int defaultValue)
            : base(defaultValue)
        {
        }

        private MaxTransactionsPerSignerPerBlockPolicy(
            MaxTransactionsPerSignerPerBlockPolicy maxTransactionsPerSignerPerBlockPolicy,
            SpannedSubPolicy<int> spannedSubPolicy)
            : base(maxTransactionsPerSignerPerBlockPolicy, spannedSubPolicy)
        {
        }

        public static IVariableSubPolicy<int> Default =>
            new MaxTransactionsPerSignerPerBlockPolicy(int.MaxValue);

        public static IVariableSubPolicy<int> Mainnet =>
            Default
                .Add(new SpannedSubPolicy<int>(
                    startIndex: BlockPolicySource.MinTransactionsPerBlockStartIndex,
                    value: BlockPolicySource.MaxTransactionsPerSignerPerBlock));
    }
}
