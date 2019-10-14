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
            // todo: 패치 전략이 수립되고 나면, 게임 내 리소스 관리에 `AddressableAsset`을 적극적으로 사용하게 될 것으로 생각됨. 지금은 사용하지 않도록 함. 
            // AddressableAssetSettings.BuildPlayerContent();
        }
    }
}
