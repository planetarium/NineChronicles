#import "AppTrackingTransparency.h"
#import <AppTrackingTransparency/AppTrackingTransparency.h>
#import <AdSupport/ASIdentifierManager.h>

void requestTrackingAuthorization(OnAuthorizationStatusDelegate callback) {
    if (@available(iOS 14.0, *)) {
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            callback((int) status);
        }];
    }
}

int trackingAuthorizationStatus() {
    if (@available(iOS 14.0, *)) {
        return (int)[ATTrackingManager trackingAuthorizationStatus];
    } else {
        return 3;
    }
}

char* advertisingIdentifier() {
    const char* idfa = [[[[ASIdentifierManager sharedManager] advertisingIdentifier] UUIDString] UTF8String];
    char* copy = (char*)malloc(strlen(idfa) + 1);
    strcpy(copy, idfa);
    return copy;
}
