typedef void (*OnAuthorizationStatusDelegate)(int status);
void requestTrackingAuthorization(OnAuthorizationStatusDelegate callback);
int trackingAuthorizationStatus();
char* advertisingIdentifier();
