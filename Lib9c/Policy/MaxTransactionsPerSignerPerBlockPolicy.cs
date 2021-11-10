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
                // Note: Introduced to prevent transactions spamming that may result in
                // the chain grinding to a halt without meaningful state transitions happening.
                // See https://github.com/planetarium/libplanet/issues/1449.
                // Issued for v100086.
                // FIXME: Starting index and value must be finalized accordingly before deployment.
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 2_800_001,
                    value: 4));

        // Note: For internal testing.
        public static IVariableSubPolicy<int> Internal =>
            Default
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 2_800_001,
                    value: 4));
    }
}
