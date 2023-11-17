//
//  AUEventParser.h
//  AirbridgeUnity
//
//  Created by WOF on 2019/11/29.
//  Copyright © 2019 ab180. All rights reserved.
//

#import <AirBridge/ABInAppEvent.h>

NS_ASSUME_NONNULL_BEGIN

@interface AUEventParser : NSObject

- (ABInAppEvent*) eventFromDictionary:(NSDictionary*)dictionary;

@end

NS_ASSUME_NONNULL_END
