//
//  AUSettingAPI.m
//  AirbridgeUnity
//
//  Created by WOF on 29/11/2019.
//

#import "AUSettingAPI.h"
#import "AirbridgeUnity.h"
#import "AUGet.h"
#import "AUConvert.h"

@interface AUSettingAPI (Internal)

+ (void) setInstance:(AUSettingAPI*)input;

@end

@implementation AUSettingAPI

static AUSettingAPI* instance;

//
// singleton
//

+ (AUSettingAPI*) instance {
    if (instance == nil) {
        instance = [[AUSettingAPI alloc] init];
    }
    
    return instance;
}

+ (void) setInstance:(AUSettingAPI*)input {
    instance = input;
}

//
// method
//

- (void) startTracking {
    [AirbridgeUnity startTracking];
}

- (void) setSessionTimeout:(uint64_t)timeout {
    [AirbridgeUnity setSessionTimeout:timeout];
}

- (void) setDeeplinkFetchTimeout:(uint64_t)timeout {
    [AirbridgeUnity setDeeplinkFetchTimeout:timeout];
}

- (void) setIsUserInfoHashed:(BOOL)enable {
    [AirbridgeUnity setIsUserInfoHashed:enable];
}

- (void) setIsTrackAirbridgeDeeplinkOnly:(BOOL)enable {
    [AirbridgeUnity setIsTrackAirbridgeDeeplinkOnly:enable];
}

- (void)registerPushToken:(NSData *)token {
    [AirbridgeUnity registerPushToken:token];
}

@end

//
// unity method
//

void native_startTracking() {
    [AUSettingAPI.instance startTracking];
}

void native_registerPushToken(const char* __nonnull token) {
    NSString *tokenString = [AUConvert stringFromChars:token];
    NSData *tokenData = [tokenString dataUsingEncoding:NSUTF8StringEncoding];
    
    if (tokenData == nil || tokenData.length == 0) { return; }
    [AirbridgeUnity registerPushToken:tokenData];
}
