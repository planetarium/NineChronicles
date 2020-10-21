using System.Linq;
using Nekoyume;
using Nekoyume.UI;
using Spine.Unity;
using UnityEngine;

namespace _Scripts.UI
{
    public class CriticalCutscene : HudWidget
    {
        private const string CUTSCENE_PATH = "Character/Cutscene/Cutscene_";

        public static void Show(int armorId)
        {
            var cutScene = Create<CriticalCutscene>(true);
            // LoadCutscene($"{CUTSCENE_PATH}/{armorId}");
            var time = LoadCutscene($"{CUTSCENE_PATH}{GameConfig.DefaultAvatarArmorId}", cutScene.gameObject);
            Destroy(cutScene.gameObject, time);
        }

        private static float LoadCutscene(string path, GameObject parent)
        {
            var origin = Resources.Load<GameObject>(path);
            var cutscene = Instantiate(origin, parent.transform);
            var animation = cutscene?.GetComponent<SkeletonAnimation>();
            var time = animation ? animation.AnimationState.Tracks.First().AnimationEnd : 0.0f;
            return time;
        }
    }
}


