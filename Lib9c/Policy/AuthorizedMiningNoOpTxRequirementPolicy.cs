namespace Nekoyume.BlockChain.Policy
{
    public static class AuthorizedMiningNoOpTxRequirementPolicy
    {
        public static VariableSubPolicy<bool> Default
        {
            get
            {
                return VariableSubPolicy<bool>
                    .Create(false);
            }
        }

        public static VariableSubPolicy<bool> Mainnet
        {
            get
            {
                return VariableSubPolicy<bool>
                    .Create(false)
                    .Add(new SpannedSubPolicy<bool>(
                        startIndex: BlockPolicySource.AuthorizedMiningNoOpTxRequirementStartIndex,
                        endIndex: BlockPolicySource.AuthorizedMiningPolicyEndIndex,
                        interval: BlockPolicySource.AuthorizedMiningPolicyInterval,
                        value: true));
            }
        }
    }
}
