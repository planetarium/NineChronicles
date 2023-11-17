//
//  AUPlacementAPI.m
//  AirbridgeUnity
//
//  Created by WOF on 29/11/2019.
//

#import "AUPlacementAPI.h"

#import <AirBridge/AirBridge.h>

#import "AUGet.h"
#import "AUConvert.h"

@interface AUPlacementAPI (Internal)

+ (void) setInstance:(AUPlacementAPI*)input;
- (instancetype)initWithPlacementAPI:(ABPlacement*)placementAPI;

@end

@implementation AUPlacementAPI {
    ABPlacement* placementAPI;
}

static AUPlacementAPI* instance;

+ (AUPlacementAPI*) instance {
    if (instance == nil) {
        instance = [[AUPlacementAPI alloc] init];
    }
    
    return instance;
}

+ (void) setInstance:(AUPlacementAPI*)input {
    instance = input;
}

- (instancetype)init {
    ABPlacement* placementAPI = AirBridge.placement;
    
    return [self initWithPlacementAPI:placementAPI];
}

- (instancetype)initWithPlacementAPI:(ABPlacement*)placementAPI {
    self = [super init];
    if (!self) {
        return nil;
    }
    
    self->placementAPI = placementAPI;
    
    return self;
}

- (void) click:(nullable const char*)trackingLinkChars {
    NSString* trackingLink = [AUConvert stringFromChars:trackingLinkChars];
    NSURL* trackingURL = [[NSURL alloc] initWithString:trackingLink];
    if (nil == trackingURL) { return; }
    
    [placementAPI click:trackingURL completion:nil];
}

- (void) impression:(nullable const char*)trackingLinkChars {
    NSString* trackingLink = [AUConvert stringFromChars:trackingLinkChars];
    NSURL* trackingURL = [[NSURL alloc] initWithString:trackingLink];
    if (nil == trackingURL) { return; }
    
    [placementAPI impression:trackingURL completion:nil];
}

@end

void native_click(const char* __nullable trackingLinkChars)
{
    [AUPlacementAPI.instance click:trackingLinkChars];
}

void native_impression(const char* __nullable trackingLinkChars)
{
    [AUPlacementAPI.instance impression:trackingLinkChars];
}
