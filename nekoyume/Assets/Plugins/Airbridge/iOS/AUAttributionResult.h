//
//  AUAttributionResult.h
//  AirbridgeUnity
//
//  Created by MinJae on 12/13/22.
//  Copyright © 2022 ab180. All rights reserved.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface AUAttributionResult : NSObject

@property (readonly, nonatomic) NSString* unityCallbackString;

+ (AUAttributionResult*)instance;

- (void)setAttributionResultCallback;
- (void)setAttributionResultCallback:(nullable const char*)objectChars;

@end

void native_setDeeplinkCallback(const char* __nullable objectChars);

NS_ASSUME_NONNULL_END
