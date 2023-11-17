//
//  AUStateAPI.m
//  AirbridgeUnity
//
//  Created by WOF on 29/11/2019.
//

#import "AUStateAPI.h"

#import <AirBridge/AirBridge.h>

#import "AUConvert.h"

NS_ASSUME_NONNULL_BEGIN

@interface AUStateAPI (Internal)

+ (void) setInstance:(AUStateAPI*)input;
- (instancetype)initWithStateAPI:(ABState*)stateAPI;

@end

@implementation AUStateAPI {
    ABState* stateAPI;
}

static AUStateAPI* instance;

//
// singleton
//

+ (AUStateAPI*) instance {
    if (instance == nil) {
        instance = [[AUStateAPI alloc] init];
    }
    
    return instance;
}

+ (void) setInstance:(AUStateAPI*)input {
    instance = input;
}

//
// init
//

- (instancetype)init {
    ABState* stateAPI = AirBridge.state;
    
    return [self initWithStateAPI:stateAPI];
}

- (instancetype)initWithStateAPI:(ABState*)stateAPI {
    self = [super init];
    if (!self) {
        return nil;
    }
    
    self->stateAPI = stateAPI;
    
    return self;
}

//
// method
//

- (void) setUserID:(nullable const char*)idChars {
    NSString* ID = [AUConvert stringFromChars:idChars];
    
    [stateAPI setUserID:ID];
}

- (void) setUserEmail:(nullable const char*)emailChars {
    NSString* email = [AUConvert stringFromChars:emailChars];
    
    [stateAPI setUserEmail:email];
}

- (void) setUserPhone:(nullable const char*)phoneChars {
    NSString* phone = [AUConvert stringFromChars:phoneChars];
    
    [stateAPI setUserPhone:phone];
}

- (void) addUserAliasWithKey:(nullable const char*)keyChars
                       value:(nullable const char*)valueChars
{
    NSString* key = [AUConvert stringFromChars:keyChars];
    NSString* value = [AUConvert stringFromChars:valueChars];
    
    [stateAPI addUserAliasWithKey:key value:value];
}

- (void) addUserAttributesWithKey:(nullable const char*)keyChars
                         intValue:(int)value
{
    NSString* key = [AUConvert stringFromChars:keyChars];
    
    [stateAPI addUserAttributesWithKey:key value:@(value)];
}

- (void) addUserAttributesWithKey:(nullable const char*)keyChars
                        longValue:(long long)value
{
    NSString* key = [AUConvert stringFromChars:keyChars];
    
    [stateAPI addUserAttributesWithKey:key value:@(value)];
}

- (void) addUserAttributesWithKey:(nullable const char*)keyChars
                       floatValue:(float)value
{
    NSString* key = [AUConvert stringFromChars:keyChars];

    [stateAPI addUserAttributesWithKey:key value:@(value)];
}

- (void) addUserAttributesWithKey:(nullable const char*)keyChars
                        boolValue:(BOOL)value
{
    NSString* key = [AUConvert stringFromChars:keyChars];
    
    [stateAPI addUserAttributesWithKey:key value:[NSNumber numberWithBool: value]];
}

- (void) addUserAttributesWithKey:(nullable const char*)keyChars
                      stringValue:(nullable const char*)valueChars
{
    NSString* key = [AUConvert stringFromChars:keyChars];
    NSString* value = [AUConvert stringFromChars:valueChars];
    
    [stateAPI addUserAttributesWithKey:key value:value];
}

- (void) clearUserAttributes {
    [stateAPI setUserAttributes:@{}];
}

- (void) expireUser {
    [stateAPI setUser:[[ABUser alloc] init]];
}

- (void) setSDKDevelopmentPlatform:(nullable const char*)platformChars
{
    NSString* platform = [AUConvert stringFromChars:platformChars];
    [stateAPI setSDKDevelopmentPlatform:platform];
}

- (void)setDeviceAliasWithKey:(NSString*)key value:(NSString*)value {
    [AirBridge.state setDeviceAliasWithKey:key value:value];
}

- (void)removeDeviceAliasWithKey:(NSString*)key {
    [AirBridge.state removeDeviceAliasWithKey:key];
}

- (void)clearDeviceAlias {
    [AirBridge.state clearDeviceAlias];
}

@end

//
// unity method
//

void native_setUserID(const char* __nullable ID) {
    [AUStateAPI.instance setUserID:ID];
}

void native_setUserEmail(const char* __nullable email) {
    [AUStateAPI.instance setUserEmail:email];
}

void native_setUserPhone(const char* __nullable phone) {
    [AUStateAPI.instance setUserPhone:phone];
}

void native_addUserAlias(const char* __nullable key, const char* __nullable value)
{
    [AUStateAPI.instance addUserAliasWithKey:key value:value];
}

void native_addUserAttributesWithInt(const char* __nullable key, int value)
{
    [AUStateAPI.instance addUserAttributesWithKey:key intValue:value];
}

void native_addUserAttributesWithLong(const char* __nullable key, long long value)
{
    [AUStateAPI.instance addUserAttributesWithKey:key longValue:value];
}

void native_addUserAttributesWithFloat(const char* __nullable key, float value)
{
    [AUStateAPI.instance addUserAttributesWithKey:key floatValue:value];
}

void native_addUserAttributesWithBOOL(const char* __nullable key, BOOL value)
{
    [AUStateAPI.instance addUserAttributesWithKey:key boolValue:value];
}

void native_addUserAttributesWithString(const char* __nullable key, const char* __nullable value)
{
    [AUStateAPI.instance addUserAttributesWithKey:key stringValue:value];
}

void native_clearUserAttributes()
{
    [AUStateAPI.instance clearUserAttributes];
}

void native_expireUser()
{
    [AUStateAPI.instance expireUser];
}

void native_setDeviceAliasWithKey(const char* __nullable key, const char* __nullable value) {
    if (key == NULL || value == NULL) { return; }
    NSString* keyString = [AUConvert stringFromChars:key];
    NSString* valueString = [AUConvert stringFromChars:value];
    
    [AUStateAPI.instance setDeviceAliasWithKey:keyString value:valueString];
}

void native_removeDeviceAliasWithKey(const char* __nullable key) {
    if (key == NULL) { return; }
    NSString* keyString = [AUConvert stringFromChars:key];
    [AUStateAPI.instance removeDeviceAliasWithKey:keyString];
}

void native_clearDeviceAlias(void) {
    [AUStateAPI.instance clearDeviceAlias];
}

NS_ASSUME_NONNULL_END
