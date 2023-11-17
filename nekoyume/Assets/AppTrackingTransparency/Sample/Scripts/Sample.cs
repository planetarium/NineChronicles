using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample : MonoBehaviour
{
    void Start()
    {
        AppTrackingTransparency.OnAuthorizationStatusReceived += OnAuthorizationStatusReceived;
        AppTrackingTransparency.AuthorizationStatus status = AppTrackingTransparency.TrackingAuthorizationStatus();
        if (status == AppTrackingTransparency.AuthorizationStatus.NotDetermined)
        {
            AppTrackingTransparency.RequestTrackingAuthorization();
        }
    }

    void Update()
    {
        
    }

    void OnAuthorizationStatusReceived(AppTrackingTransparency.AuthorizationStatus status)
    {
        AppTrackingTransparency.OnAuthorizationStatusReceived -= OnAuthorizationStatusReceived;

        // DO ANYTHING
        Debug.Log("IDFA : " + AppTrackingTransparency.AdvertisingIdentifier());
    }
}
