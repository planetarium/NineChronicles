using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Nekoyume
{
    [Serializable]
    public class TimeMachineBehaviour : PlayableBehaviour
    {
        public TimeMachineAction action;
        public TimeMachineCondition condition;
        public string markerToJumpTo, markerLabel;

        public float timeToJumpTo;
        public TimeMachineRewind rewind;

        [HideInInspector]
        public bool clipExecuted; //the user shouldn't author this, the Mixer does

        public bool ConditionMet()
        {
            switch (condition)
            {
                case TimeMachineCondition.Always:
                    return true;

                case TimeMachineCondition.IsRewind:
                    return rewind == null || rewind.IsRunning;

                case TimeMachineCondition.Never:
                default:
                    return false;
            }
        }
    }
}
