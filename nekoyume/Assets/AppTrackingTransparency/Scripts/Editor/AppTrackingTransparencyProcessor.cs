#if UNITY_IOS && !UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class AppTrackingTransparencyProcessor
{
    [PostProcessBuild]
    public static void OnPostBuild(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            AddFrameworkToProject(pathToBuiltProject, "AppTrackingTransparency.framework");
            AddFrameworkToProject(pathToBuiltProject, "AdSupport.framework");
            AddPlistElement(pathToBuiltProject, "NSUserTrackingUsageDescription",
                AppTrackingTransparencyData.GetInstance().userTrackingUsageDescription);
        }
    }

    private static void AddPlistElement(string pathToBuiltProject, string key, string value)
    {
        PlistDocument plist = new PlistDocument();
        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        plist.ReadFromString(File.ReadAllText(plistPath));
        PlistElementDict root = plist.root;
        root.SetString(key, value);
        File.WriteAllText(plistPath, plist.WriteToString());
    }

    private static void AddFrameworkToProject(string pathToBuiltProject, string framework)
    {
        string pathPBXProject = Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj/project.pbxproj");
        PBXProject project = new PBXProject();
        project.ReadFromString(File.ReadAllText(pathPBXProject));
#if UNITY_2019_3_OR_NEWER
        string guidTarget = project.GetUnityFrameworkTargetGuid();
#else
        string guidTarget = project.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif
        project.AddFrameworkToProject(guidTarget, framework, true);
        File.WriteAllText(pathPBXProject, project.WriteToString());
    }
}
#endif