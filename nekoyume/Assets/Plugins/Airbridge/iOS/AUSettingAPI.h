//
//  AUSettingAPI.h
//  AirbridgeUnity
//
//  Created by WOF on 29/11/2019.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface AUSettingAPI : NSObject

+ (AUSettingAPI*) instance;

- (void) startTracking;
- (void) registerPushToken:(NSData *)token;
- (void) setSessionTimeout:(uint64_t)timeout;
- (void) setDeeplinkFetchTimeout:(uint64_t)timeout;
- (void) setIsUserInfoHashed:(BOOL)enable;
- (void) setIsTrackAirbridgeDeeplinkOnly:(BOOL)enable;

@end

void native_startTracking(void);
void native_registerPushToken(const char* __nonnull token);

NS_ASSUME_NONNULL_END
