//
//  AUDeeplinkAPI.h
//  AirbridgeUnity
//
//  Created by WOF on 29/11/2019.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface AUDeeplinkAPI : NSObject

@property (readonly, nonatomic) NSString* initialDeeplink;

+ (AUDeeplinkAPI*) instance;

- (void) setInitialDeeplinkCallback;
- (void) setDeeplinkCallback:(nullable const char*)objectChars;

@end

void native_setDeeplinkCallback(const char* __nullable objectChars);

NS_ASSUME_NONNULL_END
