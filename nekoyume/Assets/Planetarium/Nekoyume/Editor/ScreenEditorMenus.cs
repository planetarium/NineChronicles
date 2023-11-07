using Cysharp.Threading.Tasks;
using Nekoyume.Game;
using Nekoyume.Game.Util;
using UnityEditor;
using UnityEngine;

namespace Planetarium.Nekoyume.Editor
{
    public class ScreenEditorMenuItems
    {
        [MenuItem("Screen/DynamicRatio")]
        private static void SetScreenDynamicRatio()
        {
            ActionCamera.instance.UpdateDynamicRatio();
            var raidCam = Component.FindObjectOfType<RaidCamera>();
            if (raidCam != null)
                raidCam.UpdateDynamicRatio();
        }

        [MenuItem("Screen/StaticRatio")]
        private static void SetScreenStaticRatio()
        {
            ActionCamera.instance.UpdateStaticRatioWithLetterBox();
            var raidCam = Component.FindObjectOfType<RaidCamera>();
            if (raidCam != null)
                raidCam.UpdateStaticRatioWithLetterBox();
        }
    }
}
