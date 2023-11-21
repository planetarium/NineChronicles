//
//  AUDeeplinkSender.m
//  AirbridgeUnity
//
//  Created by WOF on 2019/11/29.
//  Copyright © 2019 ab180. All rights reserved.
//

#import "AUDeeplinkSender.h"

#ifndef DEBUG
#import "UnityAppController.h"
#import "AUAppSetting.h"
#endif

@implementation AUDeeplinkSender

- (void) send:(NSString*)deeplink to:(NSString*)object {
#ifndef DEBUG
    UnitySendMessage(object.UTF8String, "OnTrackingLinkResponse", deeplink.UTF8String);
#endif
}

@end
