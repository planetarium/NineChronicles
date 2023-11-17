//
//  AUAppController.mm
//  AirbridgeUnity
//
//  Created by WOF on 2019/12/10.
//  Copyright © 2019 ab180. All rights reserved.
//

#import "UnityAppController.h"

#import "AUAppDelegate.h"

@interface AUAppController : UnityAppController

@end

@implementation AUAppController

- (BOOL) application:(UIApplication*)application 
continueUserActivity:(NSUserActivity*)userActivity 
  restorationHandler:(void (^)(NSArray<id<UIUserActivityRestoring>>* _Nullable))restorationHandler 
{
    [AUAppDelegate.instance application:application 
                   continueUserActivity:userActivity 
                     restorationHandler:restorationHandler];

    return YES;
}

@end

IMPL_APP_CONTROLLER_SUBCLASS(AUAppController)
