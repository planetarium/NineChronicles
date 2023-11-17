//
//  AUEventSender.m
//  AirbridgeUnity
//
//  Created by WOF on 2019/11/29.
//  Copyright © 2019 ab180. All rights reserved.
//

#import "AUEventSender.h"

#import <AirBridge/ABUserEvent.h>
#import <AirBridge/ABEcommerceEvent.h>

#import "AUEventKeys.h"

@implementation AUEventSender

- (void) send:(ABInAppEvent*)event {
    [event send];
}

@end
