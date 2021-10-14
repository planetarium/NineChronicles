namespace Nekoyume.BlockChain.Policy
{
    public static class MinTransactionsPerBlockPolicy
    {
        public static readonly int DefaultValue = 0;

        public static VariableSubPolicy<int> Default
        {
            get
            {
                return VariableSubPolicy<int>
                    .Create(DefaultValue);
            }
        }

        public static VariableSubPolicy<int> Mainnet
        {
            get
            {
                return Default
                    // To prevent selfish mining, we define a consensus that blocks with
                    // no transactions are not accepted starting from hard coded index.
                    .Add(new SpannedSubPolicy<int>(
                        startIndex: BlockPolicySource.MinTransactionsPerBlockStartIndex,
                        value: 1));
            }
        }
    }
}
