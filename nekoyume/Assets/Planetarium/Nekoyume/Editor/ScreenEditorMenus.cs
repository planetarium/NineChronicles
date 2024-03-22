using Cysharp.Threading.Tasks;
using Nekoyume.Game;
using Nekoyume.Game.CameraSystem;
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
            CameraManager.Instance.MainCamera.UpdateDynamicRatio();
            var raidCam = Component.FindObjectOfType<RaidCamera>();
            if (raidCam != null)
                raidCam.UpdateDynamicRatio();
        }

        [MenuItem("Screen/StaticRatio")]
        private static void SetScreenStaticRatio()
        {
            CameraManager.Instance.MainCamera.UpdateStaticRatioWithLetterBox();
            var raidCam = Component.FindObjectOfType<RaidCamera>();
            if (raidCam != null)
                raidCam.UpdateStaticRatioWithLetterBox();
        }
    }
}
