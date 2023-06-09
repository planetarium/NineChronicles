README - In-App Purchasing Sample Scenes - Buying Subscription

In this sample, you will see how to handle subscription purchases and use the `SubscriptionManager` class to retrieve information about a subscription. The `SubscriptionManager` only supports the App Store, Google Play Store, and Amazon Store.


This sample uses a fake store for its transactions, to use a real store like the App Store or the Google Play Store, you would need to register your application and add In-App Purchases. For more information, follow the documentation for one of our [supported stores](https://docs.unity3d.com/Packages/com.unity.purchasing@3.1/manual/UnityIAPSettingUp.html). Keep in mind that in this sample, product identifiers are kept in the `BuyingConsumables.cs` file.

### Subscription

Users can access the Product for a finite period of time. Subscription Products can be restored.

Examples:
* Monthly access to an online game
* VIP status granting daily bonuses
* A free trial
