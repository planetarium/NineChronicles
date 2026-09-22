namespace OneStore.Purchasing
{
    /// <summary>
    /// Represents purchase state for a ONE Store in-app purchase data.
    /// Public documentation can be found at
    /// https://onestore-dev.gitbook.io/dev/v/eng/tools/tools/v21/references/en-annotations/en-purchasedata.purchasestate
    /// </summary>
    public enum PurchaseState
    {
        PURCHASED = 0,
        CANCEL = 1,
        REFUND = 2,
    }
}
