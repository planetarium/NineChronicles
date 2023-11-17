//
//  AUAppDelegate.mm
//  AirbridgeUnity
//
//  Created by WOF on 2019/12/10.
//  Copyright © 2019 ab180. All rights reserved.
//

#import "AUAppDelegate.h"
#import "AUAppSetting.h"

#import "AirbridgeUnity.h"

@implementation AUAppDelegate

static AUAppDelegate* shared = AUAppDelegate.instance;

+ (void) initialize {
    if (shared == nil) {
        shared = AUAppDelegate.instance;
    }
}

+ (AUAppDelegate*) instance {
    if (shared == nil) {
        shared = [[AUAppDelegate alloc] init];
    }

    return shared;
}

- (instancetype) init {
    self = [super init];
    if (!self) {
        return nil;
    }

    UnityRegisterAppDelegateListener(self);

    return self;
}

- (void) didFinishLaunching:(NSNotification*)notification {
    [AirbridgeUnity autoStartTrackingEnabled:autoStartTrackingEnabled];
    [AirbridgeUnity setSessionTimeout:sessionTimeoutSeconds * 1000];
    [AirbridgeUnity setIsUserInfoHashed:userInfoHashEnabled];
    [AirbridgeUnity setIsTrackAirbridgeDeeplinkOnly:trackAirbridgeLinkOnly];
    [AirbridgeUnity setIsFacebookDeferredAppLinkEnabled:facebookDeferredAppLinkEnabled];
    [AirbridgeUnity setTrackingAuthorizeTimeout:trackingAuthorizeTimeoutSeconds * 1000];
    [AirbridgeUnity setSDKSignatureSecretWithID:sdkSignatureSecretID secret:sdkSignatureSecret];
    [AirbridgeUnity setLogLevel:logLevel];

    [AirbridgeUnity getInstance:appToken 
                        appName:appName 
              withLaunchOptions:notification.userInfo];
}

- (void) onOpenURL:(NSNotification*)notification {
    NSURL* url = notification.userInfo[@"url"];

    [AirbridgeUnity.deeplink handleURLSchemeDeeplink:url];
}

- (void) application:(UIApplication*)application 
continueUserActivity:(NSUserActivity*)userActivity 
  restorationHandler:(void (^)(NSArray<id<UIUserActivityRestoring>>* _Nullable))restorationHandler
{
    [AirbridgeUnity.deeplink handleUniversalLink:userActivity.webpageURL];
}

@end