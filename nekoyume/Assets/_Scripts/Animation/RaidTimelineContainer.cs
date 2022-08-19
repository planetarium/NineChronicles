using Nekoyume.Game.Character;
using Spine.Unity;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Nekoyume
{
    public class RaidTimelineContainer : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector director = null;

        [SerializeField]
        private RaidPlayer player = null;

        [SerializeField]
        private RaidBoss boss = null;

        [SerializeField]
        private new Camera camera = null;

        [SerializeField]
        private TimelineAsset appearCutscene = null;

        [SerializeField]
        private TimelineAsset skillCutscene = null;

        [SerializeField]
        private TimelineAsset[] runAwayCutscenes = null;

        [SerializeField]
        private TimelineAsset fallDownCutscene = null;

        [SerializeField]
        private TimelineAsset playerDefeatCutscene = null;

        public RaidPlayer Player => player;
        public RaidBoss Boss => boss;
        public Camera Camera => camera;
        public bool IsCutscenePlaying { get; private set; }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public IEnumerator CoPlayAppearCutscene() => CoPlayCutscene(appearCutscene);

        public IEnumerator CoPlaySkillCutscene() => CoPlayCutscene(skillCutscene);

        public IEnumerator CoPlayRunAwayCutscene(int wave) => CoPlayCutscene(runAwayCutscenes[wave]);

        public IEnumerator CoPlayFallDownCutscene() => CoPlayCutscene(fallDownCutscene);

        public IEnumerator CoPlayPlayerDefeatCutscene() => CoPlayCutscene(playerDefeatCutscene);

        private IEnumerator CoPlayCutscene(TimelineAsset asset)
        {
            director.GetGenericBinding(this);
            var track = asset.GetRootTracks()
                .FirstOrDefault(x => x.name.Equals("Spine_Player"))
                .GetChildTracks()
                .First();
            director.SetGenericBinding(track, player.GetComponentInChildren<SkeletonAnimation>());

            IsCutscenePlaying = true;
            director.playableAsset = asset;
            director.RebuildGraph();
            director.Play();
            yield return new WaitWhile(() => director.state == PlayState.Playing);
            director.Stop();
            IsCutscenePlaying = false;
        }

        public void ReverseX()
        {
            transform.localScale = new Vector3(
                -transform.localScale.x,
                transform.localScale.y,
                transform.localScale.z);
        }
    }
}
