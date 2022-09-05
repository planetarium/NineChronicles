using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Nekoyume
{
    [TrackClipType(typeof(TimeScaleClip))]
    public class TimeScaleTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var scriptPlayable = ScriptPlayable<TimeScaleBehaviour>.Create(graph, inputCount);
            var behaviour = scriptPlayable.GetBehaviour();
            behaviour.timeScale = 1;
            return scriptPlayable;
        }
    }
}
