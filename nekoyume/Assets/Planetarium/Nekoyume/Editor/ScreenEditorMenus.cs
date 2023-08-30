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

            ClearScreen().Forget();

            static async UniTaskVoid ClearScreen()
            {
                if (IngameDebugConsoleCommands.IngameDebugConsoleObj != null)
                {
                    var blackClearingImg = IngameDebugConsoleCommands.IngameDebugConsoleObj.transform.Find("BlackClearingImg");
                    if (blackClearingImg != null)
                    {
                        blackClearingImg.gameObject.SetActive(true);
                        await UniTask.WaitForEndOfFrame();
                        blackClearingImg.gameObject.SetActive(false);
                    }
                }
            }
        }

        [MenuItem("Screen/StaticRatio")]
        private static void SetScreenStaticRatio()
        {
            ActionCamera.instance.UpdateStaticRatioWithLetterBox();
            var raidCam = Component.FindObjectOfType<RaidCamera>();
            if (raidCam != null)
                raidCam.UpdateStaticRatioWithLetterBox();

            ClearScreen().Forget();

            static async UniTaskVoid ClearScreen()
            {
                if (IngameDebugConsoleCommands.IngameDebugConsoleObj != null)
                {
                    var blackClearingImg = IngameDebugConsoleCommands.IngameDebugConsoleObj.transform.Find("BlackClearingImg");
                    if (blackClearingImg != null)
                    {
                        blackClearingImg.gameObject.SetActive(true);
                        await UniTask.WaitForEndOfFrame();
                        blackClearingImg.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
