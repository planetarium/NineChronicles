using System.Runtime.InteropServices;
using AOT;

public class AppTrackingTransparency
{
    public enum AuthorizationStatus
    {
        NotDetermined = 0,
        Restricted,
        Denied,
        Authorized
    }

    public static event AuthorizationStatusReceived OnAuthorizationStatusReceived;
    public delegate void AuthorizationStatusReceived(AuthorizationStatus status);

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void requestTrackingAuthorization(AuthorizationStatusResponse callback);
    [DllImport("__Internal")]
    private static extern int trackingAuthorizationStatus();
    [DllImport("__Internal")]
    private static extern string advertisingIdentifier();

    private delegate void AuthorizationStatusResponse(int status);

    [MonoPInvokeCallback(typeof(AuthorizationStatusResponse))]
    private static void OnAuthorizationStatusResponse(int status)
    {
        OnAuthorizationStatusReceived?.Invoke((AuthorizationStatus)status);
    }
#endif

    public static void RequestTrackingAuthorization()
    {
#if UNITY_IOS && !UNITY_EDITOR
        requestTrackingAuthorization(OnAuthorizationStatusResponse);
#else
        OnAuthorizationStatusReceived?.Invoke(AuthorizationStatus.Authorized);
#endif
    }

    public static AuthorizationStatus TrackingAuthorizationStatus()
    {
#if UNITY_IOS && !UNITY_EDITOR
        return (AuthorizationStatus)trackingAuthorizationStatus();
#else
        return AuthorizationStatus.Authorized;
#endif
    }

    public static string AdvertisingIdentifier()
    {
#if UNITY_IOS && !UNITY_EDITOR
        return advertisingIdentifier();
#else
        return "99999999-9999-9999-9999-999999999999";
#endif
    }
}
