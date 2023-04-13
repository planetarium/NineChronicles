namespace Nekoyume.BlockChain.Policy
{
    public sealed class MinBlockProtocolVersionPolicy : VariableSubPolicy<int>
    {
        private MinBlockProtocolVersionPolicy(int defaultValue)
            : base(defaultValue)
        {
        }

        private MinBlockProtocolVersionPolicy(
            MinBlockProtocolVersionPolicy minBlockProtocolVersionPolicy,
            SpannedSubPolicy<int> spannedSubPolicy)
            : base(minBlockProtocolVersionPolicy, spannedSubPolicy)
        {
        }

        public static IVariableSubPolicy<int> Default =>
            new MinBlockProtocolVersionPolicy(0);

        public static IVariableSubPolicy<int> Mainnet =>
            Default
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 6880000,
                    endIndex: null,
                    filter: null,
                    value: 4));

        public static IVariableSubPolicy<int> Internal =>
            Default
                .Add(new SpannedSubPolicy<int>(
                    startIndex: 6384500,
                    endIndex: null,
                    filter: null,
                    value: 4));
    }
}
