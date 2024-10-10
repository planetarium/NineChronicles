using Spine.Unity;
using Spine.Unity.Playables;
using UnityEngine;
using UnityEngine.Timeline;

namespace Nekoyume.Director
{
    public static class TimelineHelper
    {
        public static void BindingSpineInstanceToTrack(TrackAsset track, SkeletonAnimation skeletonAnimation)
        {
            foreach (var timelineClip in track.GetClips())
            {
                var runtimeAsset = ScriptableObject.CreateInstance<AnimationReferenceAssetWrapper>();
                var spineClip = timelineClip.asset as SpineAnimationStateClip;
                if (spineClip == null)
                {
                    continue;
                }

                string animationName;
                var defaultAnimation = spineClip.template.animationReference;
                if (defaultAnimation == null || string.IsNullOrEmpty(defaultAnimation.name))
                {
                    // TODO: 월드보스 마지막 컷신쯤 캐릭터를 대상으로 이 로그가 반복 호출됨. 동작상 문제는 없지만 원인 파악 필요.
                    if (string.IsNullOrEmpty(timelineClip.displayName) || timelineClip.displayName == "SpineAnimationStateClip")
                    {
                        spineClip.template.animationReference = null;
                        continue;
                    }

                    animationName = timelineClip.displayName == "idle" ? "Idle" : timelineClip.displayName;
                }
                else
                {
                    animationName = defaultAnimation.name;
                }

                runtimeAsset.SetReference(animationName, skeletonAnimation.skeletonDataAsset);
                spineClip.template.animationReference = runtimeAsset;
            }
        }
    }
}
