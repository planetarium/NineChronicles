using Nekoyume.Game;
using Nekoyume.Game.Character;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace Nekoyume
{
    public class RaidTimelineContainer : MonoBehaviour
    {
        [Serializable]
        public class SkillCutsceneInfo
        {
            public int SkillId;
            public TimelineAsset Playable;
        }

        [SerializeField]
        private List<Transform> vfxList = null;

        [SerializeField]
        private Button skipButton = null;

        [SerializeField]
        private List<SkillCutsceneInfo> skillCutsceneInfos;

        [SerializeField]
        private PlayableDirector director = null;

        [SerializeField]
        private RaidPlayer player = null;

        [SerializeField]
        private RaidBoss boss = null;

        [SerializeField]
        private new RaidCamera camera = null;

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
        public RaidCamera Camera => camera;
        public bool IsCutscenePlaying { get; private set; }
        public System.Action OnAttackPoint { private get; set; }

        private Marker _currentSkipMarker = null;

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

        public bool SkillCutsceneExists(int skillId)
        {
            var info = skillCutsceneInfos.FirstOrDefault(x => x.SkillId == skillId);
            return info != null && info.Playable != null;
        }

        public void SkipCutscene()
        {
            if (_currentSkipMarker is null)
            {
                return;
            }

            skipButton.gameObject.SetActive(false);
            director.time = _currentSkipMarker.time;
            _currentSkipMarker = null;
        }

        public IEnumerator CoPlaySkillCutscene(int skillId)
        {
            var info = skillCutsceneInfos.FirstOrDefault(x => x.SkillId == skillId);
            yield return CoPlayCutscene(info.Playable);
        }

        private IEnumerator CoPlayCutscene(TimelineAsset asset)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitWhile(() => player.IsActing);
            yield return new WaitWhile(() => boss.IsActing);

            director.GetGenericBinding(this);
            var track = asset.GetRootTracks()
                .FirstOrDefault(x => x.name.Equals("Spine_Player"))
                .GetChildTracks()
                .FirstOrDefault();
            if (!track)
            {
                OnAttackPoint = null;
                yield break;
            }

            var appearance = player.GetComponent<CharacterAppearance>();
            if (appearance)
            {
                director.SetGenericBinding(track, appearance.SpineController.SkeletonAnimation);
            }

            if (asset.markerTrack)
            {
                var markers = asset.markerTrack.GetMarkers().OfType<SkipMarker>();
                _currentSkipMarker = markers.FirstOrDefault();
                if (_currentSkipMarker != null)
                {
                    skipButton.gameObject.SetActive(true);
                }
            }

            IsCutscenePlaying = true;
            director.playableAsset = asset;
            director.RebuildGraph();
            director.Play();
            yield return new WaitWhile(() => director.state == PlayState.Playing);
            OnAttackPoint = null;
            director.Stop();
            IsCutscenePlaying = false;
            skipButton.gameObject.SetActive(false);
        }

        public void ReverseX()
        {
            transform.localScale = new Vector3(
                -transform.localScale.x,
                transform.localScale.y,
                transform.localScale.z);

            foreach (var vfx in vfxList)
            {
                vfx.localScale = new Vector3(
                    -vfx.localScale.x,
                    vfx.localScale.y,
                    vfx.localScale.z);
            }
        }

        public void AttackPoint()
        {
            OnAttackPoint.Invoke();
        }

        public void KillPlayer()
        {
            player.Animator.Die();
        }
    }
}
