using IngameDebugConsole;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Nekoyume.Game.Util
{
    public class IngameDebugConsoleCommands
    {
        public static GameObject IngameDebugConsoleObj;
        public static void Initailize()
        {
            DebugLogConsole.AddCommand("screen", "Change Screen Ratio State ", () =>
            {
                ActionCamera.instance.ChangeRatioState();
                var raidCam = Component.FindObjectOfType<RaidCamera>();
                if (raidCam != null)
                    raidCam.ChangeRatioState();

                ClearScreen().Forget();
                async UniTaskVoid ClearScreen()
                {
                    if(IngameDebugConsoleObj != null)
                    {
                        var blackClearingImg = IngameDebugConsoleObj.transform.Find("BlackClearingImg");
                        if(blackClearingImg != null)
                        {
                            blackClearingImg.gameObject.SetActive(true);
                            await UniTask.WaitForEndOfFrame();
                            blackClearingImg.gameObject.SetActive(false);
                        }
                    }
                }
            });

            DebugLogConsole.AddCommand("clo","show current commandline option", ()=>{
                Game.instance.ShowCLO();
            });
        }
    }
}
