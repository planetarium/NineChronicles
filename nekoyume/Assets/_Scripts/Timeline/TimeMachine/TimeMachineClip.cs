using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Nekoyume
{
    [Serializable]
    public class TimeMachineClip : PlayableAsset, ITimelineClipAsset
    {
        [HideInInspector]
        public TimeMachineBehaviour template = new TimeMachineBehaviour();

        public TimeMachineAction action;
        public TimeMachineCondition condition;
        public ExposedReference<TimeMachineRewind> conditionChecker;
        public string markerToJumpTo = string.Empty;
        public string markerLabel = string.Empty;
        public float timeToJumpTo;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TimeMachineBehaviour>.Create(graph, template);
            var clone = playable.GetBehaviour();
            clone.rewind = conditionChecker.Resolve(graph.GetResolver());
            clone.markerToJumpTo = markerToJumpTo;
            clone.action = action;
            clone.condition = condition;
            clone.markerLabel = markerLabel;
            clone.timeToJumpTo = timeToJumpTo;

            return playable;
        }
    }
}
