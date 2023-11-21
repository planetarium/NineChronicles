//
//  AUPlacementAPI.h
//  AirbridgeUnity
//
//  Created by WOF on 29/11/2019.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface AUPlacementAPI : NSObject

+ (AUPlacementAPI*) instance;

- (void) click:(nullable const char*)trackingLinkChars;
- (void) impression:(nullable const char*)trackingLinkChars;

@end

void native_click(const char* __nullable trackingLinkChars);
void native_impression(const char* __nullable trackingLinkChars);

NS_ASSUME_NONNULL_END
