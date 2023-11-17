//
//  AUGet.h
//  AirbridgeUnity
//
//  Created by WOF on 29/11/2019.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface AUGet : NSObject

+ (nullable id) type:(Class)type object:(nullable id)object;
+ (nullable id) type:(Class)type dictionary:(nullable NSDictionary*)dictionary key:(NSString*)key;
+ (nullable id) type:(Class)type array:(nullable NSArray*)array index:(int)index;

@end

NS_ASSUME_NONNULL_END
