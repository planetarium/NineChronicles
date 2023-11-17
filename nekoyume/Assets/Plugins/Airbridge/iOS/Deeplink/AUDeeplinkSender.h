//
//  AUDeeplinkSender.h
//  AirbridgeUnity
//
//  Created by WOF on 2019/11/29.
//  Copyright © 2019 ab180. All rights reserved.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface AUDeeplinkSender : NSObject

- (void) send:(NSString*)deeplink to:(NSString*)object;

@end

NS_ASSUME_NONNULL_END
