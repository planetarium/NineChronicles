using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lib9c.DevExtensions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using AppleAuth.Editor;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
#if UNITY_STANDALONE_OSX
using UnityEditor.OSXStandalone;
#endif

namespace Editor
{
    [ExecuteInEditMode]
    public class Builder
    {
#if UNITY_ANDROID || UNITY_IOS
        private static string PlayerName = "Nine Chronicles M";
#else
        private static string PlayerName = PlayerSettings.productName;
#endif

        private const string BuildBasePath = "build";

        [MenuItem("Build/Standalone/Android Arm64")]
        public static void BuildAndroid()
        {
            SetByCommandLineArguments();
            EditorUserBuildSettings.il2CppCodeGeneration = UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            AirbridgeSettingsWindow.UpdateAndroidManifest();
            AssetDatabase.Refresh();
            Debug.Log("Build Android");
            BuildOptions options = BuildOptions.None;
            Build(BuildTarget.Android, options, "Android", false);
        }

        [MenuItem("Build/Standalone/Windows + macOS + Linux")]
        public static void BuildAll()
        {
#if UNITY_STANDALONE_OSX
            BuildStandaloneOSX();
            // BuildMacOSArm64();
#endif
            BuildStandaloneWindows();
            BuildStandaloneLinux64();
        }

#if UNITY_STANDALONE_OSX
        [MenuItem("Build/Standalone/macOS (Intel)")]
        public static void BuildStandaloneOSX()
        {
            Debug.Log("Build macOS (Intel)");
            var originalArchitecture = UserBuildSettings.architecture;
            try
            {
                UserBuildSettings.architecture = MacOSArchitecture.x64;
                Build(BuildTarget.StandaloneOSX, targetDirName: "StandaloneOSX");
            }
            finally
            {
                UserBuildSettings.architecture = originalArchitecture;
            }
        }

        /*
        // TODO: macOS Apple Silicon
        [MenuItem("Build/Standalone/macOS (Apple Silicon)")]
        public static void BuildMacOSArm64()
        {
            Debug.Log("Build macOS (Apple Silicon)");
            var originalArchitecture = UserBuildSettings.architecture;
            try
            {
                UserBuildSettings.architecture = MacOSArchitecture.ARM64;
                Build(BuildTarget.StandaloneOSX, targetDirName: "macOS (Apple Silicon)");
            }
            finally
            {
                UserBuildSettings.architecture = originalArchitecture;
            }
        }
        */
#endif

        [MenuItem("Build/Standalone/Windows")]
        public static void BuildStandaloneWindows()
        {
            Debug.Log("Build Windows");
            Build(BuildTarget.StandaloneWindows64, targetDirName: "StandaloneWindows");
        }

        [MenuItem("Build/Standalone/Linux")]
        public static void BuildStandaloneLinux64()
        {
            Debug.Log("Build Linux");
            Build(BuildTarget.StandaloneLinux64, targetDirName: "StandaloneLinux64");
        }

        [MenuItem("Build/Standalone/macOS Headless")]
        public static void BuildMacOSHeadless()
        {
            Debug.Log("Build macOS Headless");
            Build(BuildTarget.StandaloneOSX, BuildOptions.EnableHeadlessMode, "macOSHeadless");
        }

        [MenuItem("Build/Standalone/Linux Headless")]
        public static void BuildLinuxHeadless()
        {
            Debug.Log("Build Linux Headless");
            Build(BuildTarget.StandaloneLinux64, BuildOptions.EnableHeadlessMode, "LinuxHeadless");
        }

        [MenuItem("Build/Standalone/Windows Headless")]
        public static void BuildWindowsHeadless()
        {
            Debug.Log("Build Windows Headless");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.EnableHeadlessMode, "WindowsHeadless");
        }

        [MenuItem("Build/Standalone/iOS")]
        public static void BuildiOS()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            EditorUserBuildSettings.il2CppCodeGeneration = UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize;
            SetByCommandLineArguments();
            AirbridgeSettingsWindow.UpdateiOSAppSetting();
            Debug.Log("Build iOS");
            PreProcessBuildForIOS();
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            Build(BuildTarget.iOS, targetDirName: "iOS", useDevExtension: false);
        }

        [MenuItem("Build/Development/Android")]
        public static void BuildAndroidDevelopment()
        {
            Debug.Log("Build Android Development");
            Build(BuildTarget.Android, BuildOptions.Development, "Android", true);
        }

        [MenuItem("Build/Development/Windows + macOS + Linux")]
        public static void BuildAllDevelopment()
        {
            BuildMacOSDevelopment();
            BuildWindowsDevelopment();
            BuildLinuxDevelopment();
        }

        [MenuItem("Build/Development/macOS")]
        public static void BuildMacOSDevelopment()
        {
            Debug.Log("Build MacOS Development");
            Build(BuildTarget.StandaloneOSX, BuildOptions.Development | BuildOptions.AllowDebugging, "macOS");
        }

        [MenuItem("Build/Development/Windows")]
        public static void BuildWindowsDevelopment()
        {
            Debug.Log("Build Windows Development");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.AllowDebugging, "Windows");
        }

        [MenuItem("Build/Development/Linux")]
        public static void BuildLinuxDevelopment()
        {
            Debug.Log("Build Linux Development");
            Build(BuildTarget.StandaloneLinux64, BuildOptions.Development | BuildOptions.AllowDebugging, "Linux");
        }

        [MenuItem("Build/Development/macOS Headless")]
        public static void BuildMacOSHeadlessDevelopment()
        {
            Debug.Log("Build macOS Headless Development");
            Build(BuildTarget.StandaloneOSX, BuildOptions.EnableHeadlessMode, "macOSHeadless");
        }

        [MenuItem("Build/Development/Linux Headless")]
        public static void BuildLinuxHeadlessDevelopment()
        {
            Debug.Log("Build Linux Headless Development");
            Build(
                BuildTarget.StandaloneLinux64,
                BuildOptions.EnableHeadlessMode | BuildOptions.Development | BuildOptions.AllowDebugging,
                "LinuxHeadless");
        }

        [MenuItem("Build/Standalone/Windows Headless Development")]
        public static void BuildWindowsHeadlessDevelopment()
        {
            Debug.Log("Build Windows Headless Development");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.EnableHeadlessMode, "WindowsHeadless");
        }

        [MenuItem("Build/QA/Windows")]
        public static void BuildWindowsForQA()
        {
            Debug.Log("Build Windows For QA");
            CopyJsonDataFile("TestbedSell");
            CopyJsonDataFile("TestbedCreateAvatar");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.Development | BuildOptions.AllowDebugging, "Windows", true);
        }

        [MenuItem("Build/QA/iOS")]
        public static void BuildIOSForQA()
        {
            Debug.Log("Build iOS for QA");
            CopyJsonDataFile("TestbedSell");
            CopyJsonDataFile("TestbedCreateAvatar");

            PreProcessBuildForIOS();

            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;

            Build(BuildTarget.iOS, BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.WaitForPlayerConnection, "iOS-QA", true);
        }

#if UNITY_STANDALONE_OSX
        [MenuItem("Build/QA/MacOS (intel)")]
        public static void BuildMacOSForQA()
        {
            Debug.Log("Build MacOS(Intel) For QA");
            CopyJsonDataFile("TestbedSell");
            CopyJsonDataFile("TestbedCreateAvatar");
            var originalArchitecture = UserBuildSettings.architecture;
            try
            {
                UserBuildSettings.architecture = MacOSArchitecture.x64;
                Build(BuildTarget.StandaloneOSX, BuildOptions.Development | BuildOptions.AllowDebugging, "macOS", true);
            }
            finally
            {
                UserBuildSettings.architecture = originalArchitecture;
            }
        }
#endif

        private static void Build(
            BuildTarget buildTarget,
            BuildOptions options = BuildOptions.None,
            string targetDirName = null,
            bool useDevExtension = false)
        {
            string[] scenes = { "Assets/_Scenes/Game.unity" };
#if UNITY_ANDROID || UNITY_IOS
            PlayerSettings.productName = PlayerName;
#endif
            targetDirName ??= buildTarget.ToString();
            var locationPathName = Path.Combine(
                "../",
                BuildBasePath,
                targetDirName,
                buildTarget switch
                {
                    BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 =>
                        $"{PlayerName}.exe",
                    BuildTarget.Android =>
                        $"android-build.{(EditorUserBuildSettings.buildAppBundle ? "aab" : "apk")}",
                    BuildTarget.iOS => "Nine Chronicles M",
                    _ => PlayerName,
                }
            );

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPathName,
                target = buildTarget,
                options = EditorUserBuildSettings.development
                    ? options | BuildOptions.Development | BuildOptions.AllowDebugging
                    : options,
            };

            if (buildTarget == BuildTarget.Android)
            {
                // Due to executable size issue, we can't use script debugging for Android at least 2021.3.5f1.
                buildPlayerOptions.options &= ~BuildOptions.AllowDebugging;
                buildPlayerOptions.options |= BuildOptions.CompressWithLz4HC;
            }

            UpdateDefines(useDevExtension);

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
                    UpdateDefines(false);

                    // Copy readme
                    FileUtil.CopyFileOrDirectory(
                        Path.Combine("../", "README.md"),
                        Path.Combine("../", BuildBasePath, targetDirName, "README.md"));
                    FileUtil.CopyFileOrDirectory(
                        Path.Combine("../", "OSS Notice.md"),
                        Path.Combine("../", BuildBasePath, targetDirName, "OSS Notice.md"));
                    break;
                case BuildResult.Failed:
                    Debug.LogError("Build failed");
                    UpdateDefines(false);
                    break;
            }
        }

        private static void UpdateDefines(bool isDevelopment)
        {
            Debug.Log($"UpdateDefines : {isDevelopment}");
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var preDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            var newDefines = preDefines.Split(';').ToList();
            const string qaDefine = "LIB9C_DEV_EXTENSIONS";
            if (isDevelopment)
            {
                if (!newDefines.Exists(x => x.Equals(qaDefine)))
                {
                    newDefines.Add(qaDefine);
                }
            }
            else
            {
                if (newDefines.Exists(x => x.Equals(qaDefine)))
                {
                    newDefines.Remove(qaDefine);
                }
            }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup,
                string.Join(";", newDefines.ToArray()));
            EditorApplication.ExecuteMenuItem("File/Save Project");
        }

        private static void CopyJsonDataFile(string fileName)
        {
            var sourcePath = TestbedHelper.GetDataPath(fileName);
            var destPath = Path.Combine(Application.streamingAssetsPath, $"{fileName}.json");
            File.Copy(sourcePath, destPath, true);
            Debug.Log($"Copy json data file : {fileName}");
        }

        [PostProcessBuild(0)]
        public static void CopyNativeLibraries(BuildTarget target, string pathToBuiltProject)
        {
            if (target == BuildTarget.Android || target == BuildTarget.iOS)
            {
                return;
            }

            var binaryName = Path.GetFileNameWithoutExtension(pathToBuiltProject);
            var destLibPath = pathToBuiltProject;
            var libDir = "runtimes";
            switch (target)
            {
                case BuildTarget.StandaloneOSX:
                    libDir = Path.Combine(libDir, "osx-x64", "native");
                    if (!destLibPath.EndsWith(".app"))
                    {
                        destLibPath += ".app";
                    }
                    destLibPath = Path.Combine(
                        destLibPath, "Contents/Resources/Data/Managed/", libDir);
                    break;
                case BuildTarget.StandaloneWindows64:
                    libDir = Path.Combine(libDir, "win-x64", "native");
                    destLibPath = Path.Combine(
                        Path.GetDirectoryName(destLibPath), $"{binaryName}_Data/Managed", libDir);
                    break;
                case BuildTarget.StandaloneLinux64:
                    libDir = Path.Combine(libDir, "linux-x64", "native");
                    destLibPath = Path.Combine(
                        Path.GetDirectoryName(destLibPath), $"{binaryName}_Data/Managed", libDir);
                    break;
                default:
                    break;
            }

            var srcLibPath = Path.Combine(Application.dataPath, "Packages", libDir);
            var src = new DirectoryInfo(srcLibPath);
            var dest = new DirectoryInfo(destLibPath);

            Directory.CreateDirectory(dest.FullName);

            foreach (var fileInfo in src.GetFiles())
            {
                if (fileInfo.Extension == ".meta")
                {
                    continue;
                }

                fileInfo.CopyTo(Path.Combine(dest.FullName, fileInfo.Name), true);
            }
        }

#if UNITY_IOS
        [PostProcessBuild]
        public static void OnPostprocessBuildForiOS(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget != BuildTarget.iOS)
            {
                return;
            }

            var pbxProjectPath = Path.Combine(buildPath, "Unity-iPhone.xcodeproj/project.pbxproj");
            var pbxProject = new PBXProject();
            pbxProject.ReadFromFile(pbxProjectPath);

            // Disable bitcode option.
            var unityFrameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();
            pbxProject.SetBuildProperty(
                new[] { unityFrameworkTargetGuid, pbxProject.GetUnityMainTargetGuid() },
                "ENABLE_BITCODE",
                "NO");

            var targetGuid = pbxProject.GetUnityMainTargetGuid();
            // libz.tbd for grpc ios build
            pbxProject.AddFrameworkToProject(targetGuid, "libz.tbd", false);

            // Remove static frameworks at "UnityFramework".
            foreach (var framework in new []{"rocksdb.framework", "secp256k1.framework"})
            {
                var frameworkGuid = pbxProject.FindFileGuidByProjectPath($"Frameworks/Plugins/iOS/{framework}");
                pbxProject.RemoveFrameworkFromProject(unityFrameworkTargetGuid, framework);
                pbxProject.RemoveFileFromBuild(unityFrameworkTargetGuid, frameworkGuid);
            }

            // xcode 15 error
            //pbxProject.AddBuildProperty(unityFrameworkTargetGuid, "OTHER_LDFLAGS", "-ld64");

            // Re-Write project file.
            pbxProject.WriteToFile(pbxProjectPath);

            var manager = new ProjectCapabilityManager(pbxProjectPath, "Entitlements.entitlements", null, pbxProject.GetUnityMainTargetGuid());
            manager.AddSignInWithAppleWithCompatibility(pbxProject.GetUnityFrameworkTargetGuid());
            manager.AddPushNotifications(true);
            manager.WriteToFile();

            // set plist path
            var plistPath = Path.Combine(buildPath, "info.plist");

            // read plist
            Dictionary<string, object> dict;
            dict = (Dictionary<string, object>)Plist.readPlist(plistPath);

            // update plist
            dict["CFBundleURLTypes"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    {
                        "CFBundleURLSchemes", new List<object>
                        {
                            "com.googleusercontent.apps.449111430622-14152cpabg35n1squ7bq180rjptnmcvs",
                            "ninechroniclesmobile",
                            "ninechronicles-launcher",
                        }
                    }
                }
            };

            dict["ITSAppUsesNonExemptEncryption"] = false;

            dict["GIDClientID"] = "449111430622-14152cpabg35n1squ7bq180rjptnmcvs.apps.googleusercontent.com";

            // write plist
            Plist.writeXml(dict, plistPath);
        }
#endif

        private static void PreProcessBuildForIOS()
        {
            PlayerSettings.SetAdditionalIl2CppArgs("--maximum-recursive-generic-depth=30");
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
        }

        private static void CopyToBuildDirectory(string basePath, string targetDirName, string filename)
        {
            if (filename == null) return;
            var source = Path.Combine(basePath, filename);
            var destination = Path.Combine(BuildBasePath, targetDirName, filename);
            File.Copy(source, destination, true);
        }

        private static void SetByCommandLineArguments()
        {
            // This code snippets from: https://github.com/game-ci/documentation/blob/main/example/BuildScript.cs
            var cliOptions = new Dictionary<string, string>();
            var args = Environment.GetCommandLineArgs();
            // Extract flags with optional values
            for (int current = 0, next = 1; current < args.Length; current++, next++)
            {
                // Parse flag
                var isFlag = args[current].StartsWith("-");
                if (!isFlag)
                {
                    continue;
                }

                var flag = args[current].TrimStart('-');
                // Parse optional value
                var flagHasValue = next < args.Length && !args[next].StartsWith("-");
                var value = flagHasValue ? args[next].TrimStart('-') : "";
                cliOptions.Add(flag, value);

                if (cliOptions.TryGetValue("androidKeystoreName", out var keystoreName) &&
                    !string.IsNullOrEmpty(keystoreName))
                {
                    PlayerSettings.Android.useCustomKeystore = true;
                    PlayerSettings.Android.keystoreName = keystoreName;
                }

                if (cliOptions.TryGetValue("androidKeystorePass", out var keystorePass) &&
                    !string.IsNullOrEmpty(keystorePass))
                {
                    PlayerSettings.Android.keystorePass = keystorePass;
                }

                if (cliOptions.TryGetValue("androidKeyaliasName", out var keyaliasName) &&
                    !string.IsNullOrEmpty(keyaliasName))
                {
                    PlayerSettings.Android.keyaliasName = keyaliasName;
                }

                if (cliOptions.TryGetValue("androidKeyaliasPass", out var keyaliasPass) &&
                    !string.IsNullOrEmpty(keyaliasPass))
                {
                    PlayerSettings.Android.keyaliasPass = keyaliasPass;
                }

                if (cliOptions.TryGetValue("customBuildPath", out var outPath) &&
                    !string.IsNullOrEmpty(outPath))
                {
                    var aab = outPath.EndsWith(".aab");
                    EditorUserBuildSettings.buildAppBundle = aab;
                    PlayerSettings.Android.useAPKExpansionFiles = aab;
                }

                if (cliOptions.TryGetValue("identifier", out var outIdentifier) &&
                    !string.IsNullOrEmpty(outIdentifier))
                {
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, outIdentifier);
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, outIdentifier);
                }

                if (cliOptions.TryGetValue("playerName", out var outPlayerName) &&
                    !string.IsNullOrEmpty(outPlayerName))
                {
                    PlayerName = outPlayerName.Replace("-", " ");
                }
            }
        }
    }
}
