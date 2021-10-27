namespace Nekoyume.BlockChain.Policy
{
    public sealed class AuthorizedMiningNoOpTxRequiredPolicy : VariableSubPolicy<bool>
    {
        private AuthorizedMiningNoOpTxRequiredPolicy(bool defaultValue)
            : base(defaultValue)
        {
        }

        private AuthorizedMiningNoOpTxRequiredPolicy(
            AuthorizedMiningNoOpTxRequiredPolicy authorizedMiningNoOpTxRequiredPolicy,
            SpannedSubPolicy<bool> spannedSubPolicy)
            : base(authorizedMiningNoOpTxRequiredPolicy, spannedSubPolicy)
        {
        }

        public static AuthorizedMiningNoOpTxRequiredPolicy Default =>
            new AuthorizedMiningNoOpTxRequiredPolicy(false);

        public static AuthorizedMiningNoOpTxRequiredPolicy Mainnet =>
            Default
                .Add(new SpannedSubPolicy<bool>(
                    startIndex: BlockPolicySource.AuthorizedMiningNoOpTxRequiredStartIndex,
                    endIndex: BlockPolicySource.AuthorizedMinersPolicyEndIndex,
                    predicate: index => index % BlockPolicySource.AuthorizedMinersPolicyInterval == 0,
                    value: true));
    }
}
