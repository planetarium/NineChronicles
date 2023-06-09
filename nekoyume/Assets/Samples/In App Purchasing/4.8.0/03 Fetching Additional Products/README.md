README - In-App Purchasing Sample Scenes - Fetching Additional Products

In this sample, you will see how to use the `IStoreController` to fetch additional products that can be purchased in your store.

This sample uses a fake store for its transactions, to use a real store like the App Store or the Google Play Store,  you would need to register your application and add In-App Purchases. For more information, follow the documentation for one of our [supported stores](https://docs.unity3d.com/Packages/com.unity.purchasing@3.1/manual/UnityIAPSettingUp.html). Keep in mind that in this sample, product identifiers are kept in the `FetchingAdditionalProducts.cs` file.

### Fetch Additional Products
Fetch additional products allows you to fetch new in-app purchasable items dynamically after initialization.

Examples:
* Add new products dynamically without having to update the app on the store.
* Manage products from a content management system and fetch remotely.
