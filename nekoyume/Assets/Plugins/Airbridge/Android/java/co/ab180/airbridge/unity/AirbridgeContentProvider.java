package co.ab180.airbridge.unity;

import android.app.Application;
import android.content.ContentProvider;
import android.content.ContentValues;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.database.Cursor;
import android.net.Uri;
import android.os.Bundle;
import android.util.Log;

import org.jetbrains.annotations.NotNull;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Objects;

import co.ab180.airbridge.Airbridge;
import co.ab180.airbridge.AirbridgeConfig;
import co.ab180.airbridge.OnAttributionResultReceiveListener;

public class AirbridgeContentProvider extends ContentProvider {

    private static final String META_DATA_PREFIX = "airbridge==";
    
    private static final String META_DATA_APP_NAME = "co.ab180.airbridge.sdk.app_name";
    private static final String META_DATA_APP_TOKEN = "co.ab180.airbridge.sdk.app_token";
    private static final String META_DATA_SDK_SIGNATURE_SECRET_ID = "co.ab180.airbridge.sdk.sdk_signature_secret_id";
    private static final String META_DATA_SDK_SIGNATURE_SECRET = "co.ab180.airbridge.sdk.sdk_signature_secret";
    private static final String META_DATA_LOG_LEVEL = "co.ab180.airbridge.sdk.log_level";
    private static final String META_DATA_CUSTOM_DOMAIN = "co.ab180.airbridge.sdk.custom_domain";
    private static final String META_DATA_SESSION_TIMEOUT_SECONDS = "co.ab180.airbridge.sdk.session_timeout_seconds";
    private static final String META_DATA_USER_INFO_HASH_ENABLED = "co.ab180.airbridge.sdk.user_info_hash_enabled";
    private static final String META_DATA_LOCATION_COLLECTION_ENABLED = "co.ab180.airbridge.sdk.location_collection_enabled";
    private static final String META_DATA_TRACK_AIRBRIDGE_LINK_ONLY = "co.ab180.airbridge.sdk.track_airbridge_link_only";
    private static final String META_DATA_AUTO_START_TRACKING_ENABLED = "co.ab180.airbridge.sdk.auto_start_tracking_enabled";
    private static final String META_DATA_FACEBOOK_DEFERRED_APP_LINK_ENABLED = "co.ab180.airbridge.sdk.facebook_deferred_app_link_enabled";

    @Override
    public boolean onCreate() {
        try {
            Application app = (Application) Objects.requireNonNull(getContext()).getApplicationContext();
            PackageManager pm = app.getPackageManager();
            ApplicationInfo appInfo = pm.getApplicationInfo(app.getPackageName(), PackageManager.GET_META_DATA);
            Bundle bundle = appInfo.metaData;

            String appName = getActualString(bundle.getString(META_DATA_APP_NAME), META_DATA_PREFIX);
            String appToken = getActualString(bundle.getString(META_DATA_APP_TOKEN), META_DATA_PREFIX);

            AirbridgeConfig.Builder builder = new AirbridgeConfig.Builder(appName, appToken);
            builder.setPlatform("unity");
            
             if (bundle.containsKey(META_DATA_SDK_SIGNATURE_SECRET_ID) && bundle.containsKey(META_DATA_SDK_SIGNATURE_SECRET)) {
                 String sdkSignatureSecret = getActualString(bundle.getString(META_DATA_SDK_SIGNATURE_SECRET_ID, ""), META_DATA_PREFIX);
                 String sdkSignatureSecretID = getActualString(bundle.getString(META_DATA_SDK_SIGNATURE_SECRET, ""), META_DATA_PREFIX);
                 if (!sdkSignatureSecret.isEmpty() && !sdkSignatureSecretID.isEmpty()) {
                    builder.setSDKSignatureSecret(sdkSignatureSecret, sdkSignatureSecretID);
                 }
             }
             
            if (bundle.containsKey(META_DATA_LOG_LEVEL)) {
                int logLevel = bundle.getInt(META_DATA_LOG_LEVEL, 5);
                builder.setLogLevel(logLevel);
            }

            if (bundle.containsKey(META_DATA_CUSTOM_DOMAIN)) {
                String customDomain = bundle.getString(META_DATA_CUSTOM_DOMAIN, "");
                if (!customDomain.isEmpty()) {
                    List<String> customDomains = new ArrayList<>();
                    customDomains.add(customDomain);
                    builder.setCustomDomains(customDomains);
                }
            }

            if (bundle.containsKey(META_DATA_SESSION_TIMEOUT_SECONDS)) {
                long seconds = bundle.getInt(META_DATA_SESSION_TIMEOUT_SECONDS, 300);
                builder.setSessionTimeoutSeconds(seconds);
            }

            if (bundle.containsKey(META_DATA_USER_INFO_HASH_ENABLED)) {
                boolean enabled = bundle.getBoolean(META_DATA_USER_INFO_HASH_ENABLED, true);
                builder.setUserInfoHashEnabled(enabled);
            }

            if (bundle.containsKey(META_DATA_LOCATION_COLLECTION_ENABLED)) {
                boolean enabled = bundle.getBoolean(META_DATA_LOCATION_COLLECTION_ENABLED, false);
                builder.setLocationCollectionEnabled(enabled);
            }

            if (bundle.containsKey(META_DATA_TRACK_AIRBRIDGE_LINK_ONLY)) {
                boolean enabled = bundle.getBoolean(META_DATA_TRACK_AIRBRIDGE_LINK_ONLY, false);
                builder.setTrackAirbridgeLinkOnly(enabled);
            }

            if (bundle.containsKey(META_DATA_AUTO_START_TRACKING_ENABLED)) {
                boolean enabled = bundle.getBoolean(META_DATA_AUTO_START_TRACKING_ENABLED, true);
                builder.setAutoStartTrackingEnabled(enabled);
                AirbridgeUnity.setAutoStartTrackingEnabled(enabled);
            }

            if (bundle.containsKey(META_DATA_FACEBOOK_DEFERRED_APP_LINK_ENABLED)) {
                boolean enabled = bundle.getBoolean(META_DATA_FACEBOOK_DEFERRED_APP_LINK_ENABLED, false);
                builder.setFacebookDeferredAppLinkEnabled(enabled);
            }

            builder.setOnAttributionResultReceiveListener(new OnAttributionResultReceiveListener() {
            	@Override
            	public void onAttributionResultReceived(Map<String, String> result) {
            		AirbridgeUnity.processAttributionData(result);  
            	}
            });

            Airbridge.init(app, builder.build());
        } catch (Throwable throwable) {
            Log.e("Airbridge Unity", "Couldn't initialize SDK", throwable);
        }
        return true;
    }

    @Override
    public Cursor query(@NotNull Uri uri, String[] projection, String selection, String[] selectionArgs, String sortOrder) {
        return null;
    }

    @Override
    public String getType(@NotNull Uri uri) {
        return null;
    }

    @Override
    public Uri insert(@NotNull Uri uri, ContentValues values) {
        return null;
    }

    @Override
    public int delete(@NotNull Uri uri, String selection, String[] selectionArgs) {
        return 0;
    }

    @Override
    public int update(@NotNull Uri uri, ContentValues values, String selection, String[] selectionArgs) {
        return 0;
    }
    
    /**
     * Fixes an issue where a string cannot be found when it consists of only numbers.
     * @param str [prefix][actual string]
     * @return [actual string]
     */
    private String getActualString(String str, String prefix) {
        if (str != null && prefix != null) {
            int index = str.indexOf(prefix);
            if (index == 0) {
                return str.substring(index + prefix.length());
            }
        }
        return str;
    }
}