//
//  AirbridgeUnity.h
//  AirbridgeUnity
//
//  Created by WOF on 29/11/2019.
//

#import <AirBridge/ABState.h>
#import <AirBridge/ABDeeplink.h>

NS_ASSUME_NONNULL_BEGIN

@interface AirbridgeUnity : NSObject

//
// singleton
//

/**
 * Initialize Airbridge SDK and return singleton instance of AirbridgeUnity
 * @discussion  You should call this method on application:didFinishLaunchingWithOptions:
 *
 *              This method just return that when singleton instance of AirbridgeUnity already exist
 * @param       appToken
 *                  App Token
 * @param       appName
 *                  App Name in English
 * @param       launchOptions
 *                  Dictionary from application:didFinishLaunchingWithOptions:
 * @return      singleton instance of AirbridgeUnity
 */
+ (AirbridgeUnity*)getInstance:(NSString*)appToken
                       appName:(NSString*)appName
             withLaunchOptions:(nullable NSDictionary*)launchOptions;

/**
 * Return singleton instance of AirbridgeUnity
 * @discussion  this method return nil unless initialize Airbridge SDK
 * @return      singleton instance of AirbridgeUnity
 */
+ (nullable AirbridgeUnity*)instance;

//
// interface
//

/**
 * Return singleton instance of ABState
 * @discussion  you can modify user information manually with this instance
 *
 *              this method never return nil
 * @return      singleton instance of ABState
 */
+ (ABState*)state;

/**
 * Return singleton instance of ABDeeplink
 * @discussion  you can give deeplink information to Airbridge SDK with this instance
 *
 *              this method never return nil
 * @return      singleton instance of ABDeeplink
 */
+ (ABDeeplink*)deeplink;

//
// setting
//

/**
 *  Set auto start tracking enabled
 * @param       enable
 *                  auto start airbridge event tracking or not
 */
+ (void)autoStartTrackingEnabled:(BOOL)enable;

/**
 *  Start Airbridge event tracking
 */
+ (void)startTracking;

/**
 * Register a push token(token values from a register notification in Application Delegates)
 */
+ (void)registerPushToken:(NSData*)pushToken;

/**
 * Set timeout of session
 * @param       milliseconds
 *                  amount of timeout in millisecond
 */
+ (void)setSessionTimeout:(NSInteger)milliseconds;

/**
 * Set timeout of deeplink-fetch
 * @param       millisecond
 *                  amount of timeout in millisecond
 */
+ (void)setDeeplinkFetchTimeout:(NSInteger)millisecond;

/**
 * Set is Airbridge SDK hash user-infomation before send to server
 * @discussion  default value is YES
 * @param       enable
 *                  hash user-infomation or not
 */
+ (void)setIsUserInfoHashed:(BOOL)enable;

/**
 * Set is Airbridge SDK track airbridge-deeplink only
 * @discussion  default value is YES
 * @param       enable
 *                  track airbridge-deeplink only or not
 */
+ (void)setIsTrackAirbridgeDeeplinkOnly:(BOOL)enable;

/**
 *  isFacebookDeferredAppLinkEnabled fetch deferred app link from Facebook SDK
 *
 *  @discussion default value is NO
 * @param       enable
 *                  Use facebook deferred app link or not
 */
+ (void)setIsFacebookDeferredAppLinkEnabled:(BOOL)enable;

/**
 * Set tracking authorize timeout
 * @param       milliseconds
 *                  amount of timeout in millisecond
 */
+ (void)setTrackingAuthorizeTimeout:(NSInteger)milliseconds;

/**
 * sdkSignatureSecret enable sdk signature feature that protect airbridge sdk from sdk spoofing
 */
+ (void)setSDKSignatureSecretWithID:(NSString*)identifier
                             secret:(NSString*)string;

/**
 * Set log level
 * @param       level
 *                  log level in NSUInteger
 */
+ (void)setLogLevel:(NSUInteger)level;

@end

NS_ASSUME_NONNULL_END