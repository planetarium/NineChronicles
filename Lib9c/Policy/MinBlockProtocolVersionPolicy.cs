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
                    startIndex: 4_000L,
                    value: 4));
    }
}
