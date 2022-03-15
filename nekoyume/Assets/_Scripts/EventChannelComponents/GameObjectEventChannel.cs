using System;
using UnityEngine;
using UnityEngine.Events;

namespace Nekoyume.EventChannelComponents
{
    public class GameObjectEventChannel : MonoBehaviour
    {
        [Serializable]
        public enum EventType
        {
            Awake,
            OnEnable,
            Start,
            // FixedUpdate,
            // Update,
            // LateUpdate,
            OnDisable,
            OnDestroy,
        }
        
        [Serializable]
        public struct EventSetting
        {
            public EventType When;
            public UnityEvent Invoke;
        }

        [SerializeField]
        private EventSetting[] _eventSettings;

        private void Awake() => On(EventType.Awake);

        private void OnEnable() => On(EventType.OnEnable);

        private void Start() => On(EventType.Start);

        private void OnDisable() => On(EventType.OnDisable);

        private void OnDestroy() => On(EventType.OnDestroy);

        private void On(EventType eventType)
        {
            // Debug.Log($"[MonoBehaviourEventChannel] On({value})");
            foreach (var eventSetting in _eventSettings)
            {
                if (eventSetting.When != eventType)
                {
                    continue;
                }

                eventSetting.Invoke.Invoke();
            }
        }
    }
}
