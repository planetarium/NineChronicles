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
                    value: 1))
                // Note: Loosened to allow a block without proof-tx for miner.
                // See also: https://github.com/planetarium/lib9c/issues/906 and
                // https://github.com/planetarium/lib9c/pull/911
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 3_924_700,
                    value: 0
                ));
    }
}
