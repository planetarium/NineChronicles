using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Nekoyume
{
    [Serializable]
    public class TimeScaleBehaviour : PlayableBehaviour
    {
        public float timeScale;

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
                    Time.timeScale = Game.Game.DefaultTimeScale;
                    continue;
                }

                var inputPlayable = (ScriptPlayable<TimeScaleBehaviour>)playable.GetInput(i);
                var input = inputPlayable.GetBehaviour();
                Time.timeScale = input.timeScale;
            }
        }
    }
}
