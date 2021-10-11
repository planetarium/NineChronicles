namespace Nekoyume.BlockChain.Policy
{
    public static class MaxTransactionsPerBlockPolicy
    {
        public static VariableSubPolicy<int> Default
        {
            get
            {
                return VariableSubPolicy<int>
                    .Create(int.MaxValue);
            }
        }

        public static VariableSubPolicy<int> Mainnet
        {
            get
            {
                return VariableSubPolicy<int>
                    .Create(int.MaxValue)
                    .Add(new SpannedSubPolicy<int>(
                        startIndex: 0,
                        value: 100));
            }
        }
    }
}
