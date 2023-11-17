using UnityEngine;
using UnityEditor;

public class AppTrackingTransparencyWindow : EditorWindow
{
    Vector2 userTrackingUsageDescriptionScroll;

    [MenuItem("AppTrackingTransparency/Settings")]
    public static void Init()
    {
        AppTrackingTransparencyWindow window =
            (AppTrackingTransparencyWindow) GetWindow(typeof(AppTrackingTransparencyWindow), true, "App Tracking Transparency Settings");
        window.Show();
    }

    public void OnGUI()
    {
        GUILayout.Label("User Tracking Usage Description", EditorStyles.boldLabel);
        userTrackingUsageDescriptionScroll = EditorGUILayout.BeginScrollView(userTrackingUsageDescriptionScroll, GUILayout.Height(100));
        AppTrackingTransparencyData.GetInstance().userTrackingUsageDescription =
            EditorGUILayout.TextArea(AppTrackingTransparencyData.GetInstance().userTrackingUsageDescription, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }
}
