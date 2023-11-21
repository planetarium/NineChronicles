//
//  AUDeeplinkAPI.m
//  AirbridgeUnity
//
//  Created by WOF on 29/11/2019.
//

#import "AUDeeplinkAPI.h"

#import <AirBridge/AirBridge.h>

#import "AUDeeplinkSender.h"

#import "AUGet.h"
#import "AUConvert.h"

@interface AUDeeplinkAPI (Internal)

+ (void) setInstance:(AUDeeplinkAPI*)input;

@end

@implementation AUDeeplinkAPI {
    ABDeeplink* deeplinkAPI;
    AUDeeplinkSender* sender;
}

static AUDeeplinkAPI* instance;

//
// singleton
//

+ (AUDeeplinkAPI*) instance {
    if (instance == nil) {
        instance = [[AUDeeplinkAPI alloc] init];
    }
    
    return instance;
}

+ (void) setInstance:(AUDeeplinkAPI*)input {
    instance = input;
}

//
// init
//

- (instancetype)init {
    ABDeeplink* deeplinkAPI = AirBridge.deeplink;
    AUDeeplinkSender* sender = [[AUDeeplinkSender alloc] init];
    
    return [self initWithDeeplinkAPI:deeplinkAPI sender:sender];
}

- (instancetype)initWithDeeplinkAPI:(ABDeeplink*)deeplinkAPI sender:(AUDeeplinkSender*)sender {
    self = [super init];
    if (!self) {
        return nil;
    }
    
    self->deeplinkAPI = deeplinkAPI;
    self->sender = sender;
    
    return self;
}

//
// method
//

- (void) setInitialDeeplinkCallback {
    [deeplinkAPI setDeeplinkCallback:^(NSString* deeplink) {
        self->_initialDeeplink = deeplink;
    }];
}

- (void) setDeeplinkCallback:(nullable const char*)objectChars {
    NSString* object = [AUConvert stringFromChars:objectChars];

    if (_initialDeeplink != nil) {
        [sender send:_initialDeeplink to:object];
        _initialDeeplink = nil;
    }
    
    __block AUDeeplinkAPI* blockSelf = self;
    [deeplinkAPI setDeeplinkCallback:^(NSString* deeplink) {
        [blockSelf->sender send:deeplink to:object];
    }];
}

@end

//
// unity method
//

void native_setDeeplinkCallback(const char* __nullable objectChars) {
    [AUDeeplinkAPI.instance setDeeplinkCallback:objectChars];
}
