using Nekoyume.Game;
using Nekoyume.Game.Character;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Spine.Unity.Playables;
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

            // don't modify project asset in runtime
            var deepCopyAsset = Instantiate(asset);

            director.GetGenericBinding(this);
            var tracks = deepCopyAsset.GetRootTracks()?
                                      .FirstOrDefault(x => x.name.Equals("Spine_Player"))?
                                      .GetChildTracks()
                                      .ToList();

            if (tracks is null)
            {
                Debug.LogError($"[{nameof(RaidTimelineContainer)}] No Spine_Player track found");
                yield break;
            }

            var appearance = player.GetComponent<CharacterAppearance>();
            for (int i = 0; i < 11; i++)
            {
                if (tracks[i] is null)
                {
                    OnAttackPoint = null;
                    continue;
                }

                var avatarPartsType = ((AvatarPartsType)(i + 1));
                var skeletonAnimation = appearance.SpineController.GetSkeletonAnimation(avatarPartsType);
                foreach (var timelineClip in tracks[i].GetClips())
                {
                    var runtimeAsset = ScriptableObject.CreateInstance<AnimationReferenceAssetWrapper>();
                    var spineClip = timelineClip.asset as SpineAnimationStateClip;
                    if (spineClip == null)
                        continue;

                    string animationName;
                    var    defaultAnimation = spineClip.template.animationReference;
                    if (defaultAnimation == null || string.IsNullOrEmpty(defaultAnimation.name))
                    {
                        Debug.LogWarning($"[{nameof(RaidTimelineContainer)}] AnimationReferenceAsset is null or empty");
                        if (string.IsNullOrEmpty(timelineClip.displayName))
                        {
                            spineClip.template.animationReference = null;
                            continue;
                        }

                        if (timelineClip.displayName == "idle")
                        {
                            animationName = "Idle";
                        }
                        else if (timelineClip.displayName == "SpineAnimationStateClip")
                        {
                            spineClip.template.animationReference = null;
                            continue;
                        }
                        else
                        {
                            animationName = timelineClip.displayName;
                        }
                    }
                    else
                    {
                        animationName = defaultAnimation.name;
                    }

                    runtimeAsset.SetReference(animationName, skeletonAnimation.skeletonDataAsset);
                    spineClip.template.animationReference = runtimeAsset;
                }

                director.SetGenericBinding(tracks[i], skeletonAnimation);
            }

            if (deepCopyAsset.markerTrack)
            {
                var markers = deepCopyAsset.markerTrack.GetMarkers().OfType<SkipMarker>();
                _currentSkipMarker = markers.FirstOrDefault();
                if (_currentSkipMarker != null)
                {
                    skipButton.gameObject.SetActive(true);
                }
            }

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
