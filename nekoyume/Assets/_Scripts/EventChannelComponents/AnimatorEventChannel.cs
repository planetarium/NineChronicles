using System;
using UnityEngine;
using UnityEngine.Events;

namespace Nekoyume.EventChannelComponents
{
    [RequireComponent(typeof(Animator))]
    public class AnimatorEventChannel : MonoBehaviour
    {
        [Serializable]
        public struct EventSetting
        {
            public string When;
            public UnityEvent<string> Invoke;
        }

        [SerializeField]
        private EventSetting[] _eventSettings;

        public void RaiseFromAnimator(string value)
        {
            // Debug.Log($"[AnimatorEventChannel] RaiseFromAnimator({value})");
            foreach (var eventSetting in _eventSettings)
            {
                if (eventSetting.When != value)
                {
                    continue;
                }

                eventSetting.Invoke.Invoke(value);
            }
        }
    }
}
