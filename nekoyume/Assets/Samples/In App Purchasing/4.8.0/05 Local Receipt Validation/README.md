## README - In-App Purchasing Sample Scenes - Local Validation

This sample showcases how to use the cross-platform validator to do local receipt validation. The cross-platform
validator supports the Google Play Store and Apple App Store.

## Local validation

**Important:** While Unity IAP provides a local validation method, local validation is more vulnerable to fraud.
Validating sensitive transactions server-side where possible is considered best practice. For more information, please
see [Apple](https://developer.apple.com/documentation/storekit/in-app_purchase/choosing_a_receipt_validation_technique)
and [Google Play’s](https://developer.android.com/google/play/billing/security) documentation on fraud prevention.

If the content that the user is purchasing already exists on the device, the application simply needs to make a decision
about whether to unlock it.

Unity IAP provides tools to help you hide content and to validate and parse receipts through Google Play and Apple
stores.

For more information, see the [documentation](https://docs.unity3d.com/Manual/UnityIAPValidatingReceipts.html) on the
topic.

## Instructions to test this sample:

1. Have in-app purchasing correctly configured with
   the [Google Play Store](https://docs.unity3d.com/Packages/com.unity.purchasing@4.0/manual/UnityIAPGoogleConfiguration.html)
   or [Apple App Store](https://docs.unity3d.com/Packages/com.unity.purchasing@4.0/manual/UnityIAPAppleConfiguration.html).
   1. (Alternatively) For local-only Xcode testing, follow the [Apple StoreKit Testing](https://developer.apple.com/documentation/Xcode/setting-up-storekit-testing-in-xcode) process to set up a local StoreKit Configuration.
2. Set your own product's id in
   the `InAppPurchasing game object > Local Receipt Validation script > Gold Product Id field`
   or change the `GoldProductId` value in the `LocalReceiptValidation.cs` script.
3. This sample uses the `GooglePlayTangle`, `AppleTangle`, and `AppleStoreKitTestTangle` classes. To generate these classes in your project, do the
   following:
    1. Get your license key from the [Google Play Developer Console](https://play.google.com/apps/publish/). _(Skip this
       step if you do not plan on supporting the Google Play Store)_
        1. Select your app from the list.
        2. Go to "Monetization setup" under "Monetize".
        3. Copy the key from the "Licensing" section.
    2. Open the obfuscation window from `Services > In-App Purchasing > Receipt Validation Obfuscator`.
    3. Paste your Google Play key. _(Skip this step if you do not plan on supporting the Google Play Store)_
    4. Obfuscate the key. (Creates `GooglePlayTangle`, `AppleTangle`, and `AppleStoreKitTestTangle` classes in your project.)
    5. (Optional) To ensure correct revenue data, enter your key in the Analytics dashboard.
4. Add the sample scene to the build settings in the `Build Settings` window
5. Build your project for `Android`*, `iOS`, `macOS`, or `tvOS`.
    1. (Alternatively) For local-only [Apple StoreKit Testing](https://developer.apple.com/documentation/Xcode/setting-up-storekit-testing-in-xcode), follow the process to assign the StoreKit Configuration to the current Scheme.

###### *If you are on Android, make sure the `GooglePlayStore` is selected. You can change the currently selected store under `Services > In-App Purchasing > Configure` and changing the `Current Targeted Store` field.
