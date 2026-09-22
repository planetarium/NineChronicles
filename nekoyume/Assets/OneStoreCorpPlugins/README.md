# ONEstore In-app Plugin for Unity

## Overview

ONE store **In-App Integration Library *v1.3.3*** is a service that sells and charges products implemented in Android apps to users using ONE store's authentication and payment system, and settles them with the developers.

In order to pay for in-app products, it must be linked with the ONE store service (OSS) app, and the OSS app works with the ONE store payment server to conduct payments for in-app products.

### Caution

These are required libraries for using in-app purchases or check licenses.

* Assets/OneStoreCorpPlugins/Common
* Assets/OneStoreCorpPlugins/Authentication

## Do you use a proguard?

**It's already obfuscated and in aar, so add the package to the proguard rules.**

```text
# Core proGuard rules
-keep class com.gaa.sdk.base.** { *; }
-keep class com.gaa.sdk.auth.** { *; }

# Purchasing proGuard rules
-keep class com.gaa.sdk.iap.** { *; }

# Licensing proGuard rules
-keep class com.onestore.extern.licensing.** { *; }
```

## Include external dependencies

The In-app integration Unity Library is distributed with the [EDM4U(External Dependency Manager for Unity)](https://github.com/googlesamples/unity-jar-resolver).
This library is intended for use by any Unity plugin that requires access to Android-specific libraries. It provides Unity plugins the ability to declare dependencies, which are then automatically resolved and copied into your Unity project.

## How do I use In-app module?

### Authentication module

```csharp
using OneStore.Auth;

new OneStoreAuthClientImpl().LaunchSignInFlow((signInResult) => {
    if (signInResult.IsSuccessful())
        // Sign in succeeded.
    else
        // Sign in failed.
});
```

### Purchasing module

```csharp
using OneStore.Purchasing;

IPurchaseCallback callback = new IPurchaseCallback() {
    // implements method
};

// License key for your app registered in the ONE store Developer Center.
var licenseKey = "...";
var purchaseClient = new PurchaseClientImpl(licenseKey);
purchaseClient.Initialize(callback);
```

Refer to the [IAP documentation](https://onestore-dev.gitbook.io/dev/eng/tools/tools/v21/unity) for more information.

### Licensing module

```csharp
using OneStore.Alc;

ILicenseCheckCallback callback = new ILicenseCheckCallback() {
    // implements method
}

// License key for your app registered in the ONE store Developer Center.
var licenseKey = "...";
var licenseChecker = new OneStoreAppLicenseCheckerImpl(licenseKey);
licenseChecker.Initialize(callback);
```

Refer to the [ALC documentation](https://onestore-dev.gitbook.io/dev/eng/tools/alc/using-alc-sdk-v2-in-unity) for more information.

# License

```text
Copyright 2023 One store Co., Ltd.

Licensed under the Apache License, Version 2.0 (the "License"); 
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, 
software distributed under the License is distributed on an "AS IS" BASIS, 
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
See the License for the specific language governing permissions and
limitations under the License.
```
