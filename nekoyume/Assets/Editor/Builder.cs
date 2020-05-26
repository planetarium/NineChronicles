using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Xml;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.KeyStore;
using Libplanet.Net;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using UnityEditor.Callbacks;
using IEnumerator = System.Collections.IEnumerator;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace Editor
{
    public class Builder {

        public static string PlayerName = PlayerSettings.productName;

        public const string BuildBasePath = "Build";

        public static readonly string ProjectBasePath = Path.Combine(Application.dataPath, "..", "..");

        [MenuItem("Build/Standalone/Windows + macOS + Linux")]
        public static void BuildAll()
        {
            Pack(new[]
            {
                ("osx-x64", BuildTarget.StandaloneOSX, BuildOptions.None, "MacOS"),
                ("win-x64", BuildTarget.StandaloneWindows64, BuildOptions.None, "Windows"),
                (null, BuildTarget.StandaloneLinux64, BuildOptions.None, "Linux"),
            });
        }

        [MenuItem("Build/Standalone/macOS")]
        public static void BuildMacOS()
        {
            Debug.Log("Build MacOS");
            Pack("osx-x64", BuildTarget.StandaloneOSX, targetDirName: "MacOS");
        }

        [MenuItem("Build/Standalone/Windows")]
        public static void BuildWindows()
        {
            Debug.Log("Build Windows");
            Pack("win-x64", BuildTarget.StandaloneWindows64, targetDirName: "Windows");
        }

        [MenuItem("Build/Standalone/Linux")]
        public static void BuildLinux()
        {
            Debug.Log("Build Linux");
            Pack(null, BuildTarget.StandaloneLinux64, targetDirName: "Linux");
        }

        [MenuItem("Build/Standalone/macOS Headless")]
        public static void BuildMacOSHeadless()
        {
            Debug.Log("Build MacOS Headless");
            Build(BuildTarget.StandaloneOSX, BuildOptions.EnableHeadlessMode, "MacOSHeadless");
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

        [MenuItem("Build/Development/Windows + macOS + Linux")]
        public static void BuildAllDevelopment()
        {
            Pack(new[]
            {
                ("osx-x64", BuildTarget.StandaloneOSX, BuildOptions.Development, "MacOS"),
                ("win-x64", BuildTarget.StandaloneWindows64, BuildOptions.Development, "Windows"),
                (null, BuildTarget.StandaloneLinux64, BuildOptions.Development, "Linux"),
            });
        }

        [MenuItem("Build/Development/macOS")]
        public static void BuildMacOSDevelopment()
        {
            Debug.Log("Build MacOS Development");
            Pack("osx-x64", BuildTarget.StandaloneOSX, BuildOptions.Development, targetDirName: "MacOS");
        }

        [MenuItem("Build/Development/Windows")]
        public static void BuildWindowsDevelopment()
        {
            Debug.Log("Build Windows Development");
            Pack("win-x64", BuildTarget.StandaloneWindows64, BuildOptions.Development, targetDirName: "Windows");
        }

        [MenuItem("Build/Development/Linux")]
        public static void BuildLinuxDevelopment()
        {
            Debug.Log("Build Linux Development");
            Pack(null, BuildTarget.StandaloneLinux64, BuildOptions.Development, targetDirName: "Linux");
        }

        [MenuItem("Build/Development/macOS Headless")]
        public static void BuildMacOSHeadlessDevelopment()
        {
            Debug.Log("Build MacOS Headless Development");
            Build(BuildTarget.StandaloneOSX, BuildOptions.EnableHeadlessMode, "MacOSHeadless");
        }

        [MenuItem("Build/Development/Linux Headless")]
        public static void BuildLinuxHeadlessDevelopment()
        {
            Debug.Log("Build Linux Headless Development");
            Build(
                BuildTarget.StandaloneLinux64,
                BuildOptions.EnableHeadlessMode | BuildOptions.Development,
                "LinuxHeadless");
        }

        [MenuItem("Build/Standalone/Windows Headless Development")]
        public static void BuildWindowsHeadlessDevelopment()
        {
            Debug.Log("Build Windows Headless Development");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.EnableHeadlessMode, "WindowsHeadless");
        }

        public static string Build(
            BuildTarget buildTarget,
            BuildOptions options = BuildOptions.None,
            string targetDirName = null)
        {
            Prebuild();

            string[] scenes = { "Assets/_Scenes/Game.unity" };

            targetDirName = targetDirName ?? buildTarget.ToString();
            string outDir = Path.Combine(BuildBasePath, targetDirName);
            string locationPathName = Path.Combine(
                outDir,
                buildTarget.HasFlag(BuildTarget.StandaloneWindows64) ? $"{PlayerName}.exe" : PlayerName);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPathName,
                target = buildTarget,
                options = EditorUserBuildSettings.development ? options | BuildOptions.Development : options,
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.LogError("Build failed");
            }

            return outDir;
        }

        [PostProcessBuild(0)]
        public static void CopyNativeLibraries(BuildTarget target, string pathToBuiltProject)
        {
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
                default:
                    libDir = Path.Combine(libDir, "linux-x64", "native");
                    destLibPath = Path.Combine(
                        Path.GetDirectoryName(destLibPath), $"{binaryName}_Data/Managed", libDir);
                    break;
            }

            var srcLibPath = Path.Combine(Application.dataPath, "Packages", libDir);

            var src = new DirectoryInfo(srcLibPath);
            var dest = new DirectoryInfo(destLibPath);

            Directory.CreateDirectory(dest.FullName);

            foreach (FileInfo fileInfo in src.GetFiles())
            {
                if (fileInfo.Extension == ".meta")
                {
                    continue;
                }

                fileInfo.CopyTo(Path.Combine(dest.FullName, fileInfo.Name), true);
            }
        }

        private static void CopyToBuildDirectory(string basePath, string targetDirName, string filename)
        {
            if (filename == null) return;
            var source = Path.Combine(basePath, filename);
            var destination = Path.Combine(BuildBasePath, targetDirName, filename);
            File.Copy(source, destination, true);
        }

        private static void Prebuild()
        {
            Debug.Log(nameof(Prebuild));
            var genesisBlock = BlockHelper.ImportBlock(BlockHelper.GenesisBlockPath);
            var calculatedGenesis = BlockHelper.MineGenesisBlock();
            if (BlockHelper.CompareGenesisBlocks(genesisBlock, calculatedGenesis))
            {
                Debug.Log("Export new genesis-block.");
                BlockHelper.ExportBlock(calculatedGenesis, BlockHelper.GenesisBlockPath);
            }
        }

        private static (Process, string) BuildLauncher(string rid)
        {
            string solutionDir = Path.Combine("..", "NineChronicles.Launcher");
            var pi = new ProcessStartInfo
            {
                Arguments = $"publish -r {rid} -p:PublishSingleFile=true",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.Combine(solutionDir, "Launcher"),
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            };
            // FIXME: Process.Start()는 따로 PATH 검색을 안 해주는 듯. 직접 PATH 파싱을 해서 실행파일
            // 경로를 확정해야 하는데, 이마저도 POSIX와 Windows가 문법이 미묘하게 다름.  일단은 디폴트 경로로
            // 하드코딩.
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                pi.FileName = "cmd.exe";
                pi.Arguments = @"/C dotnet.exe " + pi.Arguments;
            }
            else
            {
                pi.FileName = Environment.GetEnvironmentVariable("DOTNET_PATH")
                    ?? "/usr/local/bin/dotnet";
            }
            Process p = Process.Start(pi);
            return (p, Path.Combine(solutionDir, "out", rid));
        }

        private class KeyWindow : EditorWindow
        {
            Action<PrivateKey> callback;
            IKeyStore keyStore;
            (Guid KeyId, ProtectedPrivateKey ProtectedPrivateKey)[] privateKeys;
            string[] privateKeyOptions;
            int selectedIndex;
            string passphrase;

            void OnGUI()
            {
                privateKeys = keyStore?.List()?.Select(pair => pair.ToValueTuple()).ToArray();
                privateKeyOptions = privateKeys?.Select(pair =>
                    $"{pair.Item2.Address} ({pair.Item1.ToString().ToLower()})"
                )?.Append("Create a new private key:")?.ToArray();
                selectedIndex = EditorGUILayout.Popup("Private key", selectedIndex, privateKeyOptions);
                passphrase = EditorGUILayout.PasswordField("Passphrase", passphrase) ?? string.Empty;
                bool create = selectedIndex >= privateKeyOptions.Length - 1;

                if (GUILayout.Button(create ? "Create & sign" : "Sign"))
                {
                    PrivateKey key = null;
                    if (create)
                    {
                        key = new PrivateKey();
                        keyStore.Add(ProtectedPrivateKey.Protect(key, passphrase));
                    }
                    else
                    {
                        try
                        {
                            key = privateKeys[selectedIndex].ProtectedPrivateKey.Unprotect(passphrase);
                        }
                        catch (IncorrectPassphraseException)
                        {
                            EditorUtility.DisplayDialog(
                                "Unmatched passphrase",
                                "Private key passphrase is incorrect.",
                                "Retype passphrase"
                            );
                        }
                    }

                    if (key is PrivateKey)
                    {
                        privateKeys = null;
                        privateKeyOptions = null;
                        passphrase = string.Empty;
                        selectedIndex = 0;
                        callback(key);
                        Close();
                    }
                }

                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }
            }

            public static void AskPrivateKey(IKeyStore keyStore, Action<PrivateKey> callback)
            {
                string envKey = Environment.GetEnvironmentVariable("APV_SIGNING_PRIVATE_KEY");
                if (envKey is string keyStr)
                {
                    byte[] keyBytes = null;
                    try
                    {
                        keyBytes = ByteUtil.ParseHex(keyStr);
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog(
                            "Failed to pack",
                            "APV_SIGNING_PRIVATE_KEY does not contain a valid private key (hex).\n" +
                            e.Message,
                            "OK"
                        );
                        return;
                    }

                    callback(new PrivateKey(keyBytes));
                    return;
                }

                if (!keyStore.ListIds().Any())
                {
                    EditorUtility.DisplayDialog(
                        "Failed to pack",
                        "App protocol version cannot be signed because there is no key in the key store. " +
                        "You can create one from Tools → Libplanet → Sign A New Version.",
                        "OK"
                    );
                    return;
                }

                KeyWindow keyWindow = ScriptableObject.CreateInstance<KeyWindow>();
                keyWindow.callback = callback;
                keyWindow.keyStore = keyStore;
                keyWindow.position = new Rect(Screen.width / 2, Screen.height / 2, 350, 100);
                keyWindow.titleContent = new GUIContent("Sign an app version protocol");
                keyWindow.ShowPopup();
            }

            public static void AskPrivateKey(Action<PrivateKey> callback)
            {
                AskPrivateKey(Web3KeyStore.DefaultKeyStore, callback);
            }
        }

        private static string SearchFile(string name, string directory)
        {
            IEnumerable<string> paths;
            try
            {
                paths = Directory.EnumerateFileSystemEntries(directory);
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }

            foreach (string path in paths)
            {
                if (Path.GetFileName(path).Equals(name))
                {
                    return path;
                }

                if (Directory.Exists(path))
                {
                    string subresult = SearchFile(name, path);
                    if (subresult is string)
                    {
                        return subresult;
                    }
                }
            }

            return null;
        }

        private static void CopyFiles(string srcDir, string destDir)
        {
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            foreach (string path in Directory.EnumerateFileSystemEntries(srcDir))
            {
                string basename = Path.GetFileName(path);
                string destPath = Path.Combine(destDir, basename);

                if (Directory.Exists(path))
                {
                    CopyFiles(path, destPath);
                }
                else
                {
                    File.Copy(path, destPath, true);
                }
            }
        }

        private static JsonDocumentOptions JsonDocumentOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        };

        private static void UpdateConfigFile(
            string configFilePath,
            AppProtocolVersion apv,
            PublicKey publicKey
        )
        {
            JsonDocument doc;
            try
            {
                using (FileStream stream = File.Open(configFilePath, FileMode.Open))
                {
                    doc = JsonDocument.Parse(stream, JsonDocumentOptions);
                }
            }
            catch (Exception e) when (e is JsonException || e is FileNotFoundException)
            {
                doc = JsonDocument.Parse("{\n}", JsonDocumentOptions);
            }

            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            IEnumerable<JsonProperty> rootProps = root.EnumerateObject().Where(p =>
                !p.NameEquals("appProtocolVersionToken") &&
                !p.NameEquals("trustedAppProtocolVersionSigners")
            );

            using (FileStream stream = File.Open(configFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("appProtocolVersionToken");
                writer.WriteStringValue(apv.Token);
                writer.WritePropertyName("trustedAppProtocolVersionSigners");
                writer.WriteStartArray();
                writer.WriteStringValue(ByteUtil.Hex(publicKey.Format(true)));
                writer.WriteEndArray();
                foreach (JsonProperty prop in rootProps)
                {
                    prop.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
        }

        const string MacOSDownloadUrlFormat = "https://download.nine-chronicles.com/v{0}/macOS.tar.gz";
        const string WindowsDownloadUrlFormat = "https://download.nine-chronicles.com/v{0}/Windows.zip";

        private static void Pack(
            (string LauncherRid, BuildTarget BuildTarget, BuildOptions Options, string TargetDirName)[] buildArguments
        )
        {
            KeyWindow.AskPrivateKey(privateKey =>
            {
                int latestVersion = GetLatestPublicAppProtocolVersion();
                int nextVersion = latestVersion + 1;
                string macOSUrl = string.Format(
                    CultureInfo.InvariantCulture,
                    MacOSDownloadUrlFormat,
                    nextVersion
                );
                string windowsUrl = string.Format(
                    CultureInfo.InvariantCulture,
                    WindowsDownloadUrlFormat,
                    nextVersion
                );
                DateTimeOffset timestamp = DateTimeOffset.UtcNow;
                var extra = new AppProtocolVersionExtra(macOSUrl, windowsUrl, timestamp);
                AppProtocolVersion apv =
                    AppProtocolVersion.Sign(privateKey, nextVersion, extra.Serialize());

                foreach (var buildArg in buildArguments)
                {
                    Process launcherBuild = null;
                    string launcherPath = null;

                    if (buildArg.LauncherRid is string launcherRid)
                    {
                        (launcherBuild, launcherPath) = BuildLauncher(launcherRid);
                    }

                    string outputPath = Build(buildArg.BuildTarget, buildArg.Options, buildArg.TargetDirName);
                    string streamingAssetsDir = SearchFile(
                        name: "StreamingAssets",
                        directory: outputPath
                    );
                    if (streamingAssetsDir is null)
                    {
                        EditorUtility.DisplayDialog(
                            "Failed to pack",
                            "Failed to find StreamingAssets directory.",
                            "OK"
                        );
                        return;
                    }
                    string cloPath = Path.Combine(streamingAssetsDir, "clo.json");

                    // FIXME: clo.template.json 파일이 어디 있는지 지금은 잘 안 보이는데,
                    // 나중에 CI 체계를 업데이트하면서 찾기 쉬운 곳으로 옮기든가 해야 할 듯.
                    string cloTemplatePath = Path.Combine(
                        outputPath, "..", "..", "..", ".github", "bin", "clo.json.template");
                    if (File.Exists(cloTemplatePath))
                    {
                        File.Copy(cloTemplatePath, cloPath, overwrite: true);
                    }

                    UpdateConfigFile(
                        cloPath,
                        apv,
                        privateKey.PublicKey
                    );

                    if (launcherBuild is Process proc && launcherPath is string path)
                    {
                        StreamReader stdout = proc.StandardOutput, stderr = proc.StandardError;
                        string @out = stdout.ReadToEnd(), err = stderr.ReadToEnd();
                        proc.WaitForExit();
                        int code = proc.ExitCode;
                        if (code != 0)
                        {
                            Debug.LogErrorFormat(
                                "Failed to build the Nine Chronicles Launcher (exit code: {0}).\n{1}\n{2}",
                                code, @out, err
                            );
                            EditorUtility.DisplayDialog(
                                "Failed to pack",
                                $"Failed to build the Nine Chronicles Launcher (exit code: {code}).\n{@out}\n{@err}",
                                "OK"
                            );
                            return;
                        }

                        UpdateConfigFile(
                            Path.Combine(path, "launcher.json"),
                            apv,
                            privateKey.PublicKey
                        );

                        CopyFiles(path, outputPath);
                    }
                }
            });
        }

        private static void Pack(
            string launcherRid,
            BuildTarget buildTarget,
            BuildOptions options = BuildOptions.None,
            string targetDirName = null
        )
        {
            Pack(new []
            {
                (launcherRid, buildTarget, options, targetDirName)
            });
        }

        // CDN 통하면 API를 쓸 수 없는 듯.  그래서 S3 URL을 직접 사용.  팀 내 개발자들만 쓸 거니까 괜찮지 않을지?
        private const string S3EndpointUrl = "https://9c-test.s3.ap-northeast-2.amazonaws.com/";

        private static int GetLatestPublicAppProtocolVersion()
        {
            int maxVersion = 0;

            string cont = null;

            while (true)
            {
                var url = $"{S3EndpointUrl}?list-type=2&prefix=v&delimiter=/";
                if (cont is string)
                {
                    url = $"{url}&continuation-token={Uri.EscapeDataString(cont)}";
                }
                WebRequest request = WebRequest.Create(url);
                request.Method = "GET";
                XmlDocument doc;
                using (WebResponse resp = request.GetResponse())
                {
                    doc = new XmlDocument();
                    doc.Load(resp.GetResponseStream());
                }
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("s3", "http://s3.amazonaws.com/doc/2006-03-01/");
                XmlElement docEl = doc.DocumentElement;
                XmlNodeList prefixes = docEl.SelectNodes("//s3:CommonPrefixes/s3:Prefix", nsmgr);
                foreach (XmlNode prefix in prefixes)
                {
                    string vString = prefix.InnerText.TrimStart('v').TrimEnd('/');
                    if (int.TryParse(vString, out int v) && v > maxVersion)
                    {
                        maxVersion = v;
                    }
                }

                if (!string.Equals("true", docEl.SelectSingleNode("//s3:IsTruncated", nsmgr)?.InnerText))
                {
                    break;
                }

                cont = docEl.SelectSingleNode("//s3:NextContinuationToken", nsmgr).InnerText;
            }

            Debug.LogFormat(
                $"{nameof(Builder)}.{nameof(GetLatestPublicAppProtocolVersion)}(): {{0}}",
                maxVersion
            );
            return maxVersion;
        }
    }
}
