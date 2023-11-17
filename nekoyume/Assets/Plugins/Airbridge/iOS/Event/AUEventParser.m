//
//  AUEventParser.m
//  AirbridgeUnity
//
//  Created by WOF on 2019/11/29.
//  Copyright © 2019 ab180. All rights reserved.
//

#import "AUEventParser.h"

#import <AirBridge/ABUserEvent.h>
#import <AirBridge/ABEcommerceEvent.h>

#import "AUEventKeys.h"

#import "AUGet.h"
#import "AUConvert.h"

@implementation AUEventParser

- (ABInAppEvent*) eventFromDictionary:(NSDictionary*)dictionary {
    return [self customEventFromDictionary:dictionary];
}

- (ABInAppEvent*) customEventFromDictionary:(NSDictionary*)dictionary {
    NSString* category = [AUGet type:NSString.class dictionary:dictionary key:CATEGORY];
    if (category == nil) {
        return nil;
    }
    
    ABInAppEvent* event = [[ABInAppEvent alloc] init];
    [event setCategory:category];
    addOptionToEvent(event, dictionary);
    
    return event;
}

//
// tool
//

/**
 * Add option ; to event
 * @discussion  add nil when some data has unmatch type
 * @param       event
 *                  target event
 * @param       option
 *                  dictionary which has option infomation
 */
static void addOptionToEvent(ABInAppEvent* event, NSDictionary* __nullable option) {
    if (option == nil) return;
    
    NSString* action = [AUGet type:NSString.class dictionary:option key:ACTION];
    NSString* label = [AUGet type:NSString.class dictionary:option key:LABEL];
    NSNumber* value = [AUGet type:NSNumber.class dictionary:option key:VALUE];
    NSDictionary* customs = [AUGet type:NSDictionary.class dictionary:option key:CUSTOM];
    NSDictionary* semantic = [AUGet type:NSDictionary.class dictionary:option key:SEMANTIC];
    
    [event setAction:action];
    [event setLabel:label];
    [event setValue:value];
    [event setCustoms:customs];
    [event setSemantics:semantic];
}

@end
