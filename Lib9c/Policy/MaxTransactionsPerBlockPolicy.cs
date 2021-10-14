namespace Nekoyume.BlockChain.Policy
{
    public static class MaxTransactionsPerBlockPolicy
    {
        public static readonly int DefaultValue = int.MaxValue;

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
                    .Add(new SpannedSubPolicy<int>(
                        startIndex: 0,
                        value: BlockPolicySource.MaxTransactionsPerBlock));
            }
        }
    }
}
