using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Nekoyume
{
    [Serializable]
    public class TimeScaleClip : PlayableAsset, ITimelineClipAsset
    {
        [HideInInspector]
        public TimeScaleBehaviour template = new TimeScaleBehaviour();

        public float timeScale = 1;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TimeScaleBehaviour>.Create(graph, template);
            var clone = playable.GetBehaviour();
            clone.timeScale = timeScale;

            return playable;
        }
    }
}
