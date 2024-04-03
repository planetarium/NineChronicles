using System.Linq;
using Nekoyume.Helper;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.UI
{
    public class AreaAttackCutscene : HudWidget
    {
        private static readonly int MainTexID = Shader.PropertyToID("_MainTex");

        [SerializeField]
        private SkeletonAnimation skeletonAnimation = null;

        private SkeletonAnimation SkeletonAnimation => skeletonAnimation;

        public static void Show(int armorId)
        {
            var cutScene = Create<AreaAttackCutscene>(true);
            var time = UpdateCutscene(cutScene, armorId);
            Destroy(cutScene.gameObject, time);
        }

        public float UpdateCutscene(int armorId) => UpdateCutscene(this, armorId);

        private static float UpdateCutscene(AreaAttackCutscene cutscene, int armorId)
        {
            var sprite = SpriteHelper.GetAreaAttackCutsceneSprite(armorId);

            var mpb = new MaterialPropertyBlock();
            mpb.SetTexture(MainTexID, sprite.texture);
            if (cutscene.TryGetComponent<MeshRenderer>(out var meshRenderer))
                meshRenderer.SetPropertyBlock(mpb);
            else
                NcDebug.LogError($"[{nameof(AreaAttackCutscene)}] No MeshRenderer found.");

            return cutscene.SkeletonAnimation.AnimationState.Tracks.First().AnimationEnd;
        }
    }
}
