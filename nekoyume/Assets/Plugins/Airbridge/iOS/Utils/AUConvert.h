//
//  AUConvert.h
//  AirbridgeUnity
//
//  Created by WOF on 2019/11/29.
//  Copyright © 2019 ab180. All rights reserved.
//

#import <AirBridge/ABUser.h>
#import <AirBridge/ABProduct.h>

NS_ASSUME_NONNULL_BEGIN

@interface AUConvert : NSObject

+ (nullable NSDictionary*) dictionaryFromJSONChars:(nullable const char*)jsonChars;
+ (nullable NSArray*) arrayFromJSONChars:(nullable const char*)jsonChars;
+ (nullable NSString*) stringFromChars:(nullable const char*)chars;

@end

NS_ASSUME_NONNULL_END
