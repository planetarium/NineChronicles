using Spine.Unity;
using System.Linq;
using Nekoyume.AssetBundleHelper;
using UnityEngine;

namespace Nekoyume.UI
{
    public class FullCostumeCutscene : HudWidget
    {
        private GameObject _cutscene = null;

        public void Show(int costumeId)
        {
            var time = UpdateCutscene(costumeId);
            Destroy(_cutscene, time);
        }

        private float UpdateCutscene(int costumeId)
        {
            var bundleName = "Character/FullCostumeCutscene";
            var assetPath = $"{costumeId}";
            var asset = AssetBundleLoader.LoadAssetBundle<GameObject>(bundleName, assetPath);
            if (asset == null)
            {
                throw new FailedToLoadResourceException<GameObject>(assetPath);
            }

            _cutscene = Instantiate(asset, transform);
            var skeletonAnimation = _cutscene.GetComponent<SkeletonAnimation>();
            return skeletonAnimation.AnimationState.Tracks.First().AnimationEnd;
        }
    }
}
