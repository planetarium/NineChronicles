using IngameDebugConsole;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Nekoyume.UI;

namespace Nekoyume.Game.Util
{
    public static class ScreenClear
    {
        public static async UniTaskVoid ClearScreen(bool isHorizontal)
        {
            GameObject screenBoader;
            if (isHorizontal)
            {
                screenBoader = GameObject.Find("BackGroundClearing").transform.Find("HorizontalLetterbox").gameObject;
            }
            else
            {
                screenBoader = GameObject.Find("BackGroundClearing").transform.Find("VerticalLetterBox").gameObject;
            }

            if (screenBoader != null)
            {
                screenBoader.gameObject.SetActive(true);
                await UniTask.WaitForEndOfFrame();
                screenBoader.gameObject.SetActive(false);
            }
        }
    }

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
            });

            DebugLogConsole.AddCommand("clo","show current commandline option", ()=>{
                Game.instance.ShowCLO();
            });

            DebugLogConsole.AddCommand("patrol-avatar", "Sync patrol reward avatar info", () =>
            {
                var avatarAddress = Game.instance.States.CurrentAvatarState.address;
                var agentAddress = Game.instance.States.AgentState.address;
                var patrolReward = Widget.Find<PatrolRewardPopup>().PatrolReward;
                patrolReward.LoadAvatarInfo(avatarAddress.ToHex(), agentAddress.ToHex());
            });
        }
    }
}
