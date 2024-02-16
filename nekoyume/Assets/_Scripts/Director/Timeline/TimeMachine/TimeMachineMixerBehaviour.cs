using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Nekoyume
{
    public class TimeMachineMixerBehaviour : PlayableBehaviour
    {
        public Dictionary<string, double> markerClips;
        private PlayableDirector _director;

        public override void OnPlayableCreate(Playable playable)
        {
            _director = (playable.GetGraph().GetResolver() as PlayableDirector);
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var inputCount = playable.GetInputCount();
            for (var i = 0; i < inputCount; i++)
            {
                var inputWeight = playable.GetInputWeight(i);
                if (!(inputWeight > 0f))
                {
                    continue;
                }

                var inputPlayable = (ScriptPlayable<TimeMachineBehaviour>)playable.GetInput(i);
                var input = inputPlayable.GetBehaviour();
                if (!input.clipExecuted)
                {
                    switch (input.action)
                    {
                        case TimeMachineAction.Pause:
                            if (input.ConditionMet())
                            {
                                Game.Game.instance.PauseTimeline(_director);
                                input.clipExecuted =
                                    true; //this prevents the command to be executed every frame of this clip
                            }

                            break;

                        case TimeMachineAction.JumpToTime:
                        case TimeMachineAction.JumpToMarker:
                            if (input.ConditionMet())
                            {
                                //Rewind
                                if (input.action == TimeMachineAction.JumpToTime)
                                {
                                    //Jump to time
                                    ((PlayableDirector)playable.GetGraph().GetResolver()).time = input.timeToJumpTo;
                                }
                                else
                                {
                                    //Jump to marker
                                    var time = markerClips[input.markerToJumpTo];
                                    ((PlayableDirector)playable.GetGraph().GetResolver()).time = time;
                                }

                                input.clipExecuted = false; //we want the jump to happen again!
                            }

                            break;
                    }
                }
            }
        }
    }
}
