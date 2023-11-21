//
//  AUEventAPI.m
//  AirbridgeUnity
//
//  Created by WOF on 29/11/2019.
//

#import "AUEventAPI.h"

#import "AUEventKeys.h"
#import "AUEventParser.h"
#import "AUEventSender.h"

#import "AUGet.h"
#import "AUConvert.h"

@interface AUEventAPI (Internal)

+ (void) setInstance:(AUEventAPI*)input;
- (instancetype)initWithParser:(AUEventParser*)parser sender:(AUEventSender*)sender;

@end

@implementation AUEventAPI {
    AUEventParser* parser;
    AUEventSender* sender;
}

static AUEventAPI* instance;

//
// singleton
//

+ (AUEventAPI*) instance {
    if (instance == nil) {
        instance = [[AUEventAPI alloc] init];
    }
    
    return instance;
}

+ (void) setInstance:(AUEventAPI*)input {
    instance = input;
}

//
// init
//

- (instancetype)init {
    AUEventParser* parser = [[AUEventParser alloc] init];
    AUEventSender* sender = [[AUEventSender alloc] init];
    
    return [self initWithParser:parser sender:sender];
}

- (instancetype)initWithParser:(AUEventParser*)parser sender:(AUEventSender*)sender {
    self = [super init];
    if (!self) {
        return nil;
    }
    
    self->parser = parser;
    self->sender = sender;
    
    return self;
}

//
// method
//

- (void) sendEvent:(nullable const char*)jsonChars {
    NSDictionary* dictionary = [AUConvert dictionaryFromJSONChars:jsonChars];
    if (dictionary == nil) {
        return;
    }
    
    ABInAppEvent* event = [parser eventFromDictionary:dictionary];
    [sender send:event];
}

@end

//
// unity method
//

void native_sendEvent(const char* __nullable jsonChars) {
    [AUEventAPI.instance sendEvent:jsonChars];
}
