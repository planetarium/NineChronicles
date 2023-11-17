//
//  AUConvert.m
//  AirbridgeUnity
//
//  Created by WOF on 2019/11/29.
//  Copyright © 2019 ab180. All rights reserved.
//

#import "AUConvert.h"

#import "AUGet.h"

@implementation AUConvert

+ (nullable NSDictionary*) dictionaryFromJSONChars:(nullable const char*)jsonChars {
    NSString* string = [AUConvert stringFromChars:jsonChars];
    if (string == nil) {
        return nil;
    }
    
    NSData* data = [string dataUsingEncoding:NSUTF8StringEncoding];
    if (data == nil) {
        return nil;
    }
    
    NSError* error = nil;
    NSDictionary* dictionary = [NSJSONSerialization JSONObjectWithData:data
                                                               options:0
                                                                 error:&error];
    
    if (dictionary == nil || ![dictionary isKindOfClass:NSDictionary.class]) {
        return nil;
    }
    
    return dictionary;
}

+ (nullable NSArray*) arrayFromJSONChars:(nullable const char*)jsonChars {
    NSString* string = [AUConvert stringFromChars:jsonChars];
    if (string == nil) {
        return nil;
    }
    
    NSData* data = [string dataUsingEncoding:NSUTF8StringEncoding];
    if (data == nil) {
        return nil;
    }
    
    NSError* error = nil;
    NSArray* array = [NSJSONSerialization JSONObjectWithData:data
                                                     options:0
                                                       error:&error];
    
    if (array == nil || ![array isKindOfClass:NSArray.class]) {
        return nil;
    }
    
    return array;
}

+ (nullable NSString*) stringFromChars:(nullable const char*)chars {
    if (chars == NULL) {
        return nil;
    }
    
    NSString* string = [NSString stringWithUTF8String:chars];
    
    return string;
}

@end
