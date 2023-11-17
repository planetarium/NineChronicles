//
//  AirbridgeUnity.h
//  AirbridgeUnity
//
//  Created by WOF on 29/11/2019.
//

#import "AirbridgeUnity.h"

#import <AirBridge/AirBridge.h>

#import "AUDeeplinkAPI.h"
#import "AUStateAPI.h"

@implementation AirbridgeUnity

static AirbridgeUnity* instance;

//
// singleton
//

+ (AirbridgeUnity*) getInstance:(NSString *)appToken
                        appName:(NSString *)appName
              withLaunchOptions:(NSDictionary *)launchOptions
{
    [AUStateAPI.instance setSDKDevelopmentPlatform:"unity"];

    [AirBridge getInstance:appToken 
                   appName:appName 
         withLaunchOptions:launchOptions];
    [AUDeeplinkAPI.instance setInitialDeeplinkCallback];
    
    if (instance != nil) {
        instance = [[AirbridgeUnity alloc] init];
    }
    
    return instance;
}

+ (nullable AirbridgeUnity*)instance {
    return instance;
}

+ (void) setInstance:(AirbridgeUnity*)input {
    instance = input;
}

//
// interface
//

+ (ABState*)state {
    return AirBridge.state;
}

+ (ABDeeplink*)deeplink {
    return AirBridge.deeplink;
}

//
// setting
//

+ (void)autoStartTrackingEnabled:(BOOL)enable {
    AirBridge.autoStartTrackingEnabled = enable;
}

+ (void)startTracking {
    [AirBridge startTracking];
}

+ (void) registerPushToken:(NSData*)pushToken {
    [AirBridge registerPushToken:pushToken];
}

+ (void)setSessionTimeout:(NSInteger)milliseconds {
    [AirBridge setSessionTimeout:milliseconds];
}

+ (void)setDeeplinkFetchTimeout:(NSInteger)millisecond {
    [AirBridge.deeplink setHandleTrackingLinkTimeout:millisecond];
}

+ (void)setIsUserInfoHashed:(BOOL)enable {
    [AirBridge setIsUserInfoHashed:enable];
}

+ (void)setIsTrackAirbridgeDeeplinkOnly:(BOOL)enable {
    [AirBridge setIsTrackAirbridgeDeeplinkOnly:enable];
}

+ (void)setIsFacebookDeferredAppLinkEnabled:(BOOL)enable {
    [AirBridge setIsFacebookDeferredAppLinkEnabled:enable];
}

+ (void)setTrackingAuthorizeTimeout:(NSInteger)milliseconds {
    AirBridge.setting.trackingAuthorizeTimeout = milliseconds;
}

+ (void)setSDKSignatureSecretWithID:(NSString*)identifier
                             secret:(NSString*)string {
    if (identifier.length != 0 && string.length != 0) {
        [AirBridge setSDKSignatureSecretWithID:identifier secret:string];
    }
}

+ (void)setLogLevel:(NSUInteger)level {
    [AirBridge setLogLevel:(ABLogLevel)level];
}

@end