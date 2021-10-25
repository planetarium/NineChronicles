namespace Nekoyume.BlockChain.Policy
{
    public sealed class MinTransactionsPerBlockPolicy : VariableSubPolicy<int>
    {
        private MinTransactionsPerBlockPolicy(int defaultValue)
            : base(defaultValue)
        {
        }

        private MinTransactionsPerBlockPolicy(
            MinTransactionsPerBlockPolicy minTransactionsPerBlockPolicy,
            SpannedSubPolicy<int> spannedSubPolicy)
            : base(minTransactionsPerBlockPolicy, spannedSubPolicy)
        {
        }

        public static IVariableSubPolicy<int> Default =>
            new MinTransactionsPerBlockPolicy(0);

        public static IVariableSubPolicy<int> Mainnet =>
            Default
                // To prevent selfish mining, we define a consensus that blocks with
                // no transactions are not accepted starting from hard coded index.
                .Add(new SpannedSubPolicy<int>(
                    startIndex: BlockPolicySource.MinTransactionsPerBlockStartIndex,
                    value: 1));
    }
}
