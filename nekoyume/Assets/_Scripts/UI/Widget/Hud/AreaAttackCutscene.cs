using System.Linq;
using Nekoyume.Helper;
using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using UnityEngine;

namespace Nekoyume.UI
{
    public class AreaAttackCutscene : HudWidget
    {
        [SerializeField] private SkeletonAnimation skeletonAnimation = null;

        private const string AttachmentName = "cutscene_01";
        private const string SlotName = "cutscene";

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

            var shader = Shader.Find("Sprites/Default");
            var material = new Material(shader);

            var slot       = cutscene.SkeletonAnimation.skeleton.FindSlot(SlotName);
            var slotIndex  = slot == null ? -1 : slot.Data.Index;
            var attachment = slot?.Attachment.GetRemappedClone(sprite, material);

            var defaultSkin = cutscene.SkeletonAnimation.skeleton.Data.DefaultSkin;
            var clonedSkin = new Skin($"{defaultSkin.Name} Clone");
            clonedSkin.CopySkin(defaultSkin);

            clonedSkin.SetAttachment(slotIndex, AttachmentName, attachment);
            cutscene.SkeletonAnimation.skeleton.SetSkin(clonedSkin);

            return cutscene.SkeletonAnimation.AnimationState.Tracks.First().AnimationEnd;
        }
    }
}
