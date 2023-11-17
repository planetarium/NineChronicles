using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class AirbridgeUnity
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void native_startTracking();
    [DllImport ("__Internal")]
    private static extern void native_setUserID(string userID);
    [DllImport ("__Internal")]
    private static extern void native_setUserEmail(string userEmail);
    [DllImport ("__Internal")]
    private static extern void native_setUserPhone(string userPhone);
    [DllImport ("__Internal")]
    private static extern void native_addUserAlias(string key, string value);
    [DllImport("__Internal")]
    private static extern void native_addUserAttributesWithInt(string key, int value);
    [DllImport("__Internal")]
    private static extern void native_addUserAttributesWithLong(string key, long value);
    [DllImport("__Internal")]
    private static extern void native_addUserAttributesWithFloat(string key, float value);
    [DllImport("__Internal")]
    private static extern void native_addUserAttributesWithBOOL(string key, bool value);
    [DllImport("__Internal")]
    private static extern void native_addUserAttributesWithString(string key, string value);
    [DllImport("__Internal")]
    private static extern void native_clearUserAttributes();
    [DllImport("__Internal")]
    private static extern void native_expireUser();
    [DllImport("__Internal")]
    private static extern void native_click(string trackingLink);
    [DllImport("__Internal")]
    private static extern void native_impression(string trackingLink);
    [DllImport("__Internal")]
    private static extern void native_setDeeplinkCallback(string objectName);
    [DllImport("__Internal")]
    private static extern void native_setAttributionResultCallback(string objectName);
    [DllImport("__Internal")]
    private static extern void native_sendEvent(string json);
    [DllImport("__Internal")]
    private static extern void native_registerPushToken(string token);
    [DllImport("__Internal")]
    private static extern void native_setDeviceAliasWithKey(string key, string value);
    [DllImport("__Internal")]
    private static extern void native_removeDeviceAliasWithKey(string key);
    [DllImport("__Internal")]
    private static extern void native_clearDeviceAlias();

    public static void StartTracking()
    {
        native_startTracking();
    }

    public static void SetUser(AirbridgeUser user)
    {
        ExpireUser();

        native_setUserID(user.GetId());
        native_setUserEmail(user.GetEmail());
        native_setUserPhone(user.GetPhoneNumber());

        Dictionary<string, string> userAlias = user.GetAlias();
        foreach (string key in userAlias.Keys)
        {
            native_addUserAlias(key, userAlias[key]);
        }
        Dictionary<string, object> attrs = user.GetAttributes();
        foreach (string key in attrs.Keys)
        {
            object value = attrs[key];
            if (value is int)
            {
                native_addUserAttributesWithInt(key, (int)value);
            }
            else if (value is long)
            {
                native_addUserAttributesWithLong(key, (long)value);
            }
            else if (value is float)
            {
                native_addUserAttributesWithFloat(key, (float)value);
            }
            else if (value is bool)
            {
                native_addUserAttributesWithBOOL(key, (bool)value);
            }
            else if (value is string)
            {
                native_addUserAttributesWithString(key, (string)value);
            }
            else
            {
                Debug.LogWarning("Invalid 'user-attribute' value data type received. The value will ignored");
            }
        }
    }

    public static void ExpireUser()
    {
        native_expireUser();
    }


    public static void ClickTrackingLink(string trackingLink, string deeplink = null, string fallback = null)
    {
        native_click(trackingLink);
    }

    public static void ImpressionTrackingLink(string trackingLink)
    {
        native_impression(trackingLink);
    }

    public static void SetDeeplinkCallback(string callbackObjectName)
    {
        native_setDeeplinkCallback(callbackObjectName);
    }

    public static void SetOnAttributionReceived(string callbackObjectName)
    {
        native_setAttributionResultCallback(callbackObjectName);
    }

    public static void TrackEvent(AirbridgeEvent @event)
    {
        native_sendEvent(@event.ToJsonString());
    }
    
    public static void SetDeviceAlias(string key, string value)
    {
        native_setDeviceAliasWithKey(key, value);
    }

    public static void RemoveDeviceAlias(string key)
    {
        native_removeDeviceAliasWithKey(key);
    }

    public static void ClearDeviceAlias()
    {
        native_clearDeviceAlias();
    }
    
    public static void RegisterPushToken(string token)
    {
        native_registerPushToken(token);
    }
    
    public static AirbridgeWebInterface CreateWebInterface(string webToken, PostCommandFunction postCommandFunction)
    {
        return new AirbridgeWebInterfaceImpl(webToken, postCommandFunction);
    }
#elif UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject airbridge = new AndroidJavaObject("co.ab180.airbridge.unity.AirbridgeUnity");

    public static void StartTracking()
    {
        airbridge.CallStatic("startTracking");
    }

    public static void SetUser(AirbridgeUser user)
    {
        ExpireUser();

        airbridge.CallStatic("setUserId", user.GetId());
        airbridge.CallStatic("setUserEmail", user.GetEmail());
        airbridge.CallStatic("setUserPhone", user.GetPhoneNumber());
        Dictionary<string, string> alias = user.GetAlias();
        foreach (string key in alias.Keys)
        {
            airbridge.CallStatic("setUserAlias", key, alias[key]);
        }
        Dictionary<string, object> attrs = user.GetAttributes();
        foreach (string key in attrs.Keys)
        {
            object value = attrs[key];
            if (value is int)
            {
                airbridge.CallStatic("setUserAttribute", key, (int)value);
            }
            else if (value is long)
            {
                airbridge.CallStatic("setUserAttribute", key, (long)value);
            }
            else if (value is float)
            {
                airbridge.CallStatic("setUserAttribute", key, (float)value);
            }
            else if (value is bool)
            {
                airbridge.CallStatic("setUserAttribute", key, (bool)value);
            }
            else if (value is string)
            {
                airbridge.CallStatic("setUserAttribute", key, (string)value);
            }
            else
            {
                Debug.LogWarning("Invalid 'user-attribute' value data type received. The value will ignored");
            }
        }
    }

    public static void ExpireUser()
    {
        airbridge.CallStatic("expireUser");
    }

    public static void ClickTrackingLink(string trackingLink, string deeplink = null, string fallback = null)
    {
        airbridge.CallStatic("clickTrackingLink", trackingLink);
    }

    public static void ImpressionTrackingLink(string trackingLink)
    {
        airbridge.CallStatic("impressionTrackingLink", trackingLink);
    }

    public static void SetDeeplinkCallback(string callbackObjectName)
    {
        airbridge.CallStatic("setDeeplinkCallback", callbackObjectName);
    }

    public static void SetOnAttributionReceived(string callbackObjectName)
    {
        airbridge.CallStatic("setAttributionResultCallback", callbackObjectName);
    }

    public static void TrackEvent(AirbridgeEvent @event)
    {
        string jsonString = @event.ToJsonString();
        airbridge.CallStatic("trackEvent", jsonString);
    }
    
    public static void SetDeviceAlias(string key, string value)
    {
        airbridge.CallStatic("setDeviceAlias", key, value);
    }

    public static void RemoveDeviceAlias(string key)
    {
        airbridge.CallStatic("removeDeviceAlias", key);
    }

    public static void ClearDeviceAlias()
    {
        airbridge.CallStatic("clearDeviceAlias");
    }
    
    public static void RegisterPushToken(string token)
    {
        airbridge.CallStatic("registerPushToken", token);
    }
    
    public static AirbridgeWebInterface CreateWebInterface(string webToken, PostCommandFunction postCommandFunction)
    {
        return new AirbridgeWebInterfaceImpl(webToken, postCommandFunction);
    }
#else
    public static void StartTracking()
    {
        Debug.Log("Airbridge is not implemented this method on this platform");
    }


    public static void SetUser(AirbridgeUser user)
    {
        Debug.Log("Airbridge is not implemented this method on this platform");
    }

    public static void ExpireUser()
    {
        Debug.Log("Airbridge is not implemented this method on this platform");
    }

    public static void ClickTrackingLink(string trackingLink, string deeplink = null, string fallback = null)
    {
        Debug.Log("Airbridge is not implemented this method on this platform");
    }

    public static void ImpressionTrackingLink(string trackingLink)
    {
        Debug.Log("Airbridge is not implemented this method on this platform");
    }

    // When you register your deeplink callback, 'Airbridge' will call "void OnTrackingLinkResponse(string url)" method
    public static void SetDeeplinkCallback(string callbackObjectName)
    {
        Debug.Log("Airbridge is not implemented this method on this platform");
    }
    
    // When you register your attribution result callback, 'Airbridge' will call "void OnAttributionResultReceived(string jsonString)" method
    public static void SetOnAttributionReceived(string callbackObjectName)
    {
        Debug.Log("Airbridge is not implemented this method on this platform");
    }

    public static void TrackEvent(AirbridgeEvent @event)
    {
        Debug.Log("Airbridge is not implemented this method on this platform");
    }

    public static void SetDeviceAlias(string key, string value)
    {
        Debug.Log("Airbridge is not implemented this method on this platform");
    }

    public static void RemoveDeviceAlias(string key)
    {
        Debug.Log("Airbridge is not implemented this method on this platform");
    }

    public static void ClearDeviceAlias()
    {
        Debug.Log("Airbridge is not implemented this method on this platform");
    }

    public static void RegisterPushToken(string token)
    {
        Debug.Log("Airbridge is not implemented this method on this platform");
    }
    
    public static AirbridgeWebInterface CreateWebInterface(string webToken, PostCommandFunction postCommandFunction)
    {
        Debug.Log("Airbridge is not implemented this method on this platform");
        return new AirbridgeWebInterfaceDefault();
    }
#endif
}