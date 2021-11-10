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
                // Note: Introduced to prevent selfish mining where certain miners were
                // only mining empty blocks.  Issued for v100050.
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 2_173_701,
                    value: 1));
    }
}
