using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace Nekoyume.Director
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

        [SerializeField]
        private Canvas raidCanvas;

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

        public void SkipBattle()
        {
            Game.Game.instance.RaidStage.SkipBattle();
        }

        public IEnumerator CoPlaySkillCutscene(int skillId)
        {
            var info = skillCutsceneInfos.FirstOrDefault(x => x.SkillId == skillId);
            yield return CoPlayCutscene(info.Playable);
        }

        #region PlayTimelineCutscene
        private const int NumOfCharacterSlots = 11;
        private const int NumOfBossSlots      = 1;

        private IEnumerator CoPlayCutscene(TimelineAsset asset)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitWhile(() => player.IsActing);
            yield return new WaitWhile(() => boss.IsActing);

            // don't modify project asset in runtime
            var deepCopyAsset = Instantiate(asset);
            director.GetGenericBinding(this);

            EnableSkipMarker(deepCopyAsset);
            BindCharacterToTimeline(deepCopyAsset);
            BindBossToTimeline(deepCopyAsset);

            IsCutscenePlaying      = true;
            director.playableAsset = deepCopyAsset;
            director.RebuildGraph();
            director.Play();
            yield return new WaitWhile(() => director.state == PlayState.Playing);
            OnAttackPoint = null;
            director.Stop();
            IsCutscenePlaying = false;
            skipButton.gameObject.SetActive(false);
        }

        private void BindCharacterToTimeline(TimelineAsset timelineAsset)
        {
            var tracks = timelineAsset.GetRootTracks()?
                                      .FirstOrDefault(x => x.name.Equals("Spine_Player"))?
                                      .GetChildTracks()
                                      .ToList();

            if (tracks == null)
            {
                NcDebug.LogError($"[{nameof(RaidTimelineContainer)}] No Spine_Player track found");
                return;
            }

            var appearance = player.GetComponent<CharacterAppearance>();
            for (int i = 0; i < NumOfCharacterSlots; i++)
            {
                if (tracks[i] == null)
                {
                    OnAttackPoint = null;
                    continue;
                }

                var avatarPartsType = ((AvatarPartsType)(i + 1));
                var skeletonAnimation = appearance.SpineController.GetSkeletonAnimation(avatarPartsType);
                TimelineHelper.BindingSpineInstanceToTrack(tracks[i], skeletonAnimation);

                director.SetGenericBinding(tracks[i], skeletonAnimation);
            }

            if (!timelineAsset.markerTrack) return;

            var markers = timelineAsset.markerTrack.GetMarkers().OfType<SkipMarker>();
            _currentSkipMarker = markers.FirstOrDefault();
            if (_currentSkipMarker != null)
                skipButton.gameObject.SetActive(true);
        }

        private void BindBossToTimeline(TimelineAsset timelineAsset)
        {
            var tracks = timelineAsset.GetRootTracks()?
                                      .FirstOrDefault(x => x.name.Equals("Spine_Boss"))?
                                      .GetChildTracks()
                                      .ToList();

            if (tracks == null)
            {
                NcDebug.LogError($"[{nameof(RaidTimelineContainer)}] No Spine_Boss track found");
                return;
            }

            var skeletonAnimation = boss.GetComponent<SkeletonAnimation>();
            for (int i = 0; i < NumOfBossSlots; i++)
            {
                if (tracks[i] == null)
                {
                    continue;
                }

                TimelineHelper.BindingSpineInstanceToTrack(tracks[i], skeletonAnimation);
                director.SetGenericBinding(tracks[i], skeletonAnimation);
            }
        }

        private void EnableSkipMarker(TimelineAsset timelineAsset)
        {
            if (!timelineAsset.markerTrack) return;

            var markers = timelineAsset.markerTrack.GetMarkers().OfType<SkipMarker>();
            _currentSkipMarker = markers.FirstOrDefault();
            if (_currentSkipMarker != null)
            {
                skipButton.gameObject.SetActive(true);
            }
        }
        #endregion PlayTimelineCutscene

        public void ReverseX()
        {
            transform.localScale = new Vector3(
                -transform.localScale.x,
                transform.localScale.y,
                transform.localScale.z);

            if (raidCanvas != null)
            {
                for (int i = 0; i < raidCanvas.transform.childCount; i++)
                {
                    var canvasChildTansform = raidCanvas.transform.GetChild(i);
                    canvasChildTansform.transform.localScale = new Vector3(
                        -canvasChildTansform.transform.localScale.x,
                        canvasChildTansform.transform.localScale.y,
                        canvasChildTansform.transform.localScale.z);
                }
                if (skipButton.TryGetComponent<RectTransform>(out var skipBtnRect))
                {
                    FlipRectTransformWithAnchor(skipBtnRect);
                }
            }

            foreach (var vfx in vfxList)
            {
                vfx.localScale = new Vector3(
                    -vfx.localScale.x,
                    vfx.localScale.y,
                    vfx.localScale.z);
            }

            player.IsFlipped = transform.localScale.x < 0;
        }

        private static void FlipRectTransformWithAnchor(RectTransform rectTrans)
        {
            Vector2 anchoredPosition = rectTrans.anchoredPosition;
            anchoredPosition.x = -anchoredPosition.x;
            rectTrans.anchoredPosition = anchoredPosition;

            Vector2 anchorMin = rectTrans.anchorMin;
            Vector2 anchorMax = rectTrans.anchorMax;
            float temp = anchorMin.x;
            anchorMin.x = 1 - anchorMax.x;
            anchorMax.x = 1 - temp;
            rectTrans.anchorMin = anchorMin;
            rectTrans.anchorMax = anchorMax;
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
