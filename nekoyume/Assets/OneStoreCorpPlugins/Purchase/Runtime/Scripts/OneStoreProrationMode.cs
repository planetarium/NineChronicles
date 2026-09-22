namespace OneStore.Purchasing
{
    /// <summary>
    /// Proration mode supported by ONE Store.
    /// Public documentation can be found at
    /// https://onestore-dev.gitbook.io/dev/v/eng/tools/tools/v21/references/en-annotations/en-purchaseflowparams.prorationmode
    /// </summary>
    public enum OneStoreProrationMode
    {
        UNKNOWN_SUBSCRIPTION_UPGRADE_DOWNGRADE_POLICY = 0,
        /// <summary>
        /// Replacement takes effect immediately, and the new expiration time is proportionally distributed
        /// and deposited or charged to the user.
        /// This is the current default behavior.
        /// </summary>
        IMMEDIATE_WITH_TIME_PRORATION = 1,
        /// <summary>
        /// Replacement will take effect immediately and the billing cycle will remain the same.
        /// You will be charged for the rest of the period.
        /// This option is only available for subscription upgrades.
        /// </summary>
        IMMEDIATE_AND_CHARGE_PRORATED_PRICE = 2,
        /// <summary>
        /// Replacement will take effect immediately and a new price will be charged on the next payment date.
        /// The billing cycle remains the same.
        /// </summary>
        IMMEDIATE_WITHOUT_PRORATION = 3,
        /// <summary>
        /// When the existing plan expires, the replacement will take effect and the new plan will be
        /// charged at the same time.
        /// </summary>
        DEFERRED = 4,
    }
}
