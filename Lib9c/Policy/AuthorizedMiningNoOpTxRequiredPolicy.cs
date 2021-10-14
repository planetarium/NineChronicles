namespace Nekoyume.BlockChain.Policy
{
    public static class AuthorizedMiningNoOpTxRequiredPolicy
    {
        public static readonly bool DefaultValue = false;

        public static VariableSubPolicy<bool> Default
        {
            get
            {
                return VariableSubPolicy<bool>.Create(DefaultValue);
            }
        }

        public static VariableSubPolicy<bool> Mainnet
        {
            get
            {
                return Default
                    .Add(new SpannedSubPolicy<bool>(
                        startIndex: BlockPolicySource.AuthorizedMiningNoOpTxRequiredStartIndex,
                        endIndex: BlockPolicySource.AuthorizedMinersPolicyEndIndex,
                        predicate: index => index % BlockPolicySource.AuthorizedMinersPolicyInterval == 0,
                        value: true));
            }
        }
    }
}
