README - In-App Purchasing Sample Scenes - Integrating Self-Provided Backend Receipt Validation

In this sample, you will learn how to integrate your own backend validation with Unity IAP by
using `PurchaseProcessingResult.Pending` and confirming the purchase later. For more information see
the [documentation](https://docs.unity3d.com/Manual/UnityIAPProcessingPurchases.html) on this topic.

This sample uses a mock for the backend implementation. You can plug in your own backend by replacing
the `MockServerSideValidation` method in `IntegratingSelfProvidedBackendReceiptValidation.cs`.
For more information about how to do a web request in unity, see
the [documentation](https://docs.unity3d.com/2021.3/Documentation/ScriptReference/Networking.UnityWebRequest.Post.html).

This sample uses a fake store for its transactions, to use a real store like the App Store or the Google Play Store, you
would need to register your application and add In-App Purchases. For more information, follow the documentation for one
of our [supported stores](https://docs.unity3d.com/Packages/com.unity.purchasing@3.1/manual/UnityIAPSettingUp.html).
Keep in mind that in this sample, product identifiers are kept in
the `IntegratingSelfProvidedBackendReceiptValidation.cs` file.
