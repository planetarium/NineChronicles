#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

#ifdef __cplusplus
extern "C" {
#endif

    void _OpenURL(const char* url) {
        NSString* urlString = [NSString stringWithUTF8String:url];
        NSURL* nsUrl = [NSURL URLWithString:urlString];

        if (@available(iOS 10.0, *)) {
            [[UIApplication sharedApplication] openURL:nsUrl options:@{} completionHandler:^(BOOL success) {
                if (!success) {
                    NSLog(@"Failed to open URL: %@", urlString);
                }
            }];
        } else {
            BOOL success = [[UIApplication sharedApplication] openURL:nsUrl];
            if (!success) {
                NSLog(@"Failed to open URL: %@", urlString);
            }
        }
    }

#ifdef __cplusplus
}
#endif
