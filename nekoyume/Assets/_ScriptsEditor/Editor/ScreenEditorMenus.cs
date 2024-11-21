using Cysharp.Threading.Tasks;
using Nekoyume.Game;
using Nekoyume.Game.Util;
using UnityEditor;
using UnityEngine;

namespace NekoyumeEditor
{
    public class ScreenEditorMenuItems
    {
        [MenuItem("Screen/DynamicRatio")]
        private static void SetScreenDynamicRatio()
        {
            ActionCamera.instance.UpdateDynamicRatio();
            var raidCam = Object.FindObjectOfType<RaidCamera>();
            if (raidCam != null)
            {
                raidCam.UpdateDynamicRatio();
            }
        }

        [MenuItem("Screen/StaticRatio")]
        private static void SetScreenStaticRatio()
        {
            ActionCamera.instance.UpdateStaticRatioWithLetterBox();
            var raidCam = Object.FindObjectOfType<RaidCamera>();
            if (raidCam != null)
            {
                raidCam.UpdateStaticRatioWithLetterBox();
            }
        }
    }
}
