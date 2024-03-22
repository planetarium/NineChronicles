using IngameDebugConsole;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Nekoyume.Game.CameraSystem;
using Nekoyume.UI;

namespace Nekoyume.Game.Util
{
    public static class ScreenClear
    {
        public static void ClearScreen()
        {
            var screenBoaderH = GameObject.Find("BackGroundClearing").transform.Find("HorizontalLetterbox").gameObject;
            var screenBoaderV = GameObject.Find("BackGroundClearing").transform.Find("VerticalLetterBox").gameObject;
            if (screenBoaderH != null)
                screenBoaderH.SetActive(false);
            if (screenBoaderV != null)
                screenBoaderV.SetActive(false);
        }
        public static void ClearScreen(bool isHorizontal, float letterBoxSize)
        {
            var screenBoaderH = GameObject.Find("BackGroundClearing").transform.Find("HorizontalLetterbox").gameObject;
            var screenBoaderV = GameObject.Find("BackGroundClearing").transform.Find("VerticalLetterBox").gameObject;

            if (screenBoaderH != null)
            {
                screenBoaderH.SetActive(isHorizontal);
                if (isHorizontal)
                {
                    RectTransform rt = screenBoaderH.GetComponent<RectTransform>();
                    rt.offsetMax = new Vector2(rt.offsetMax.x, -letterBoxSize);
                    rt.offsetMin = new Vector2(rt.offsetMin.x, letterBoxSize);
                }
            }

            if (screenBoaderV != null)
            {
                screenBoaderV.SetActive(!isHorizontal);
                if (!isHorizontal)
                {
                    RectTransform rt = screenBoaderV.GetComponent<RectTransform>();
                    rt.offsetMax = new Vector2(-letterBoxSize, rt.offsetMax.y);
                    rt.offsetMin = new Vector2(letterBoxSize, rt.offsetMin.y);
                }
            }

            BlackScreenClearing().Forget();

            async UniTaskVoid BlackScreenClearing()
            {
                var blackscreenImageH = screenBoaderH.GetComponent<UnityEngine.UI.Image>();
                var blackscreenImageV = screenBoaderV.GetComponent<UnityEngine.UI.Image>();
                blackscreenImageH.color = Color.black;
                blackscreenImageV.color = Color.black;
                await UniTask.DelayFrame(2);
                blackscreenImageH.color = new Color(0, 0, 0, 0);
                blackscreenImageV.color = new Color(0, 0, 0, 0);
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
                CameraManager.Instance.MainCamera.ChangeRatioState();
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
