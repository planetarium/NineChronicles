//
//  AUEventAPI.h
//  AirbridgeUnity
//
//  Created by WOF on 29/11/2019.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface AUEventAPI : NSObject

+ (AUEventAPI*) instance;

- (void) sendEvent:(nullable const char*)jsonChars;

@end

void native_sendEvent(const char* __nullable jsonChars);

NS_ASSUME_NONNULL_END
