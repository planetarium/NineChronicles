using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BTAI
{
    public class SendSignal : StateMachineBehaviour
    {

        public string signal = "";
        [Range(0, 1)]
        public float time = 0;
        public bool fired = false;
        List<WaitForAnimatorSignal> listeners = new List<WaitForAnimatorSignal>();

        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            fired = false;
            SetFalse();
        }

        private void SetFalse()
        {
            foreach (var n in listeners)
                n.isSet = false;
        }

        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            SetFalse();
        }

        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!fired && stateInfo.normalizedTime >= time)
            {
                foreach (var n in listeners)
                    n.isSet = true;
                fired = true;
            }

        }

        public static void Register(Animator animator, string name, WaitForAnimatorSignal node)
        {
            var found = false;
            foreach (var ss in animator.GetBehaviours<SendSignal>())
            {
                if (ss.signal == name)
                {
                    found = true;
                    ss.listeners.Add(node);
                }
            }
            if (!found) Debug.LogError("Signal does not exist in animator: " + name);
        }
    }
}