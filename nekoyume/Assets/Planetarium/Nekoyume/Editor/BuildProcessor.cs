using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Planetarium.Nekoyume.Editor
{
    public class BuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; }
        public void OnPreprocessBuild(BuildReport report)
        {
            AddressableAssetSettings.BuildPlayerContent();
        }
    }
}
