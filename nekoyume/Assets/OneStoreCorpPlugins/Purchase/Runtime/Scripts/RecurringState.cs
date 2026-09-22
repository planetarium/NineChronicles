namespace OneStore.Purchasing
{
    /// <summary>
    /// Represents recurring state for a ONE Store in-app purchase data.
    /// Public documentation can be found at
    /// https://onestore-dev.gitbook.io/dev/v/eng/tools/tools/v21/references/en-annotations/en-purchasedata.recurringstate
    /// </summary>
    public enum RecurringState
    {
        RECURRING = 0,
        CANCEL = 1,
        NON_AUTO_PRODUCT = -1,
    }
}
