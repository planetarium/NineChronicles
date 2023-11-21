//
//  AUAttributionResult.m
//  AirbridgeUnity
//
//  Created by MinJae on 12/13/22.
//  Copyright © 2022 ab180. All rights reserved.
//

#import "AUAttributionResult.h"
#import "AUConvert.h"

#import <AirBridge/AirBridge.h>

#ifndef DEBUG
#import "UnityAppController.h"
#import "AUAppSetting.h"
#endif

@implementation AUAttributionResult


static AUAttributionResult* instance;
+ (AUAttributionResult *)instance {
    if (instance == nil) {
        instance = [AUAttributionResult new];
    }
    
    return instance;
}

- (void)setAttributionResultCallback {
    [AirBridge.setting setAttributionCallback:^(NSDictionary<NSString *,NSString *> * _Nonnull attribution) {
        NSString *convertString = [self attributionObjectToNSString:attribution];
        if (nil == convertString) { return; }
        
        self->_unityCallbackString = convertString;
    }];
}

- (void)setAttributionResultCallback:(const char *)objectChars {
    NSString* object = [AUConvert stringFromChars:objectChars];
    
    if (nil != self.unityCallbackString) {
        [self send:self.unityCallbackString toUnityObject:object];
        _unityCallbackString = nil;
    }
    
    [AirBridge.setting setAttributionCallback:^(NSDictionary<NSString *,NSString *> * _Nonnull attribution) {
        NSString *convertString = [self attributionObjectToNSString:attribution];
        if (nil == convertString) { return; }
        
        [self send:convertString toUnityObject:object];
    }];
}

- (void)send:(NSString *)unityCallbackString toUnityObject:(NSString *)object {
    #ifndef DEBUG
        UnitySendMessage(object.UTF8String, "OnAttributionResultReceived", unityCallbackString.UTF8String);
    #endif
}

- (NSString *)attributionObjectToNSString:(NSDictionary<NSString *,NSString *> * _Nonnull )attribution {
    if (0 == attribution.count) { return nil; }
    NSData* jsonData = [NSJSONSerialization
                        dataWithJSONObject:attribution
                        options:NSJSONWritingFragmentsAllowed
                        error:nil
    ];
    
    return [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
}

void native_setAttributionResultCallback(const char* __nullable objectChars) {
    [AUAttributionResult.instance setAttributionResultCallback:objectChars];
}

@end
