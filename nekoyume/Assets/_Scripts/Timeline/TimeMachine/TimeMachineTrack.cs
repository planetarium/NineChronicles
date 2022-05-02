using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;
using System.Globalization;

namespace Nekoyume
{
    [TrackColor(0, 1, 0.1073523f)]
    [TrackClipType(typeof(TimeMachineClip))]
    public class TimeMachineTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var scriptPlayable = ScriptPlayable<TimeMachineMixerBehaviour>.Create(graph, inputCount);
            var behaviour = scriptPlayable.GetBehaviour();
            behaviour.markerClips = new Dictionary<string, double>();

            foreach (var c in GetClips())
            {
                var clip = (TimeMachineClip)c.asset;
                string clipName;

                switch (clip.action)
                {
                    case TimeMachineAction.Pause:
                        clipName = "||";
                        break;

                    case TimeMachineAction.Marker:
                        clipName = "[*] " + clip.markerLabel.ToString();

                        if (!behaviour.markerClips.ContainsKey(clip.markerLabel))
                        {
                            behaviour.markerClips.Add(clip.markerLabel, (double)c.start);
                        }

                        break;

                    case TimeMachineAction.JumpToMarker:
                        clipName = "[JTM] " + clip.markerToJumpTo.ToString();
                        break;

                    case TimeMachineAction.JumpToTime:
                        clipName = "[JTT] " + clip.timeToJumpTo.ToString(CultureInfo.InvariantCulture);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                c.displayName = clipName;
            }

            return scriptPlayable;
        }
    }
}
