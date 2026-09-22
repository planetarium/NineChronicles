namespace OneStore.Common
{
    /// <summary>
    /// It is a constant that represents the type of store where the app is installed.
    /// </summary>
    public enum StoreType
    {
        /// <summary>
        /// Unable to determine the store (APK sideloaded, unknown source)
        /// </summary>
        UNKNOWN = 0,

        /// <summary>
        /// Installed from ONE Store (or a trusted store defined in Developer Options)
        /// </summary>
        ONESTORE = 1,

        /// <summary>
        /// Installed from Google Play Store
        /// </summary>
        VENDING = 2,

        /// <summary>
        /// Installed from other stores (Samsung Galaxy Store, Amazon Appstore, etc.)
        /// </summary>
        ETC = 3,
    }
}
