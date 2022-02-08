using System;
using Nekoyume.Game.Character;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Nekoyume.EventChannelComponents
{
    using UniRx;

    [RequireComponent(typeof(TouchHandler))]
    public class TouchHandlerEventChannel : MonoBehaviour
    {
        [Serializable]
        public struct EventSetting
        {
            public TouchHandler.EventType When;
            public UnityEvent<PointerEventData> Invoke;
        }

        private TouchHandler _touchHandler;

        [SerializeField]
        private EventSetting[] _eventSettings;

        private void Awake()
        {
            _touchHandler = GetComponent<TouchHandler>();
            _touchHandler.OnEnter
                .Subscribe(eventData => OnEvent(eventData, TouchHandler.EventType.Enter))
                .AddTo(gameObject);
            _touchHandler.OnLeftDown
                .Subscribe(eventData => OnEvent(eventData, TouchHandler.EventType.LeftDown))
                .AddTo(gameObject);
            _touchHandler.OnMiddleDown
                .Subscribe(eventData => OnEvent(eventData, TouchHandler.EventType.MiddleDown))
                .AddTo(gameObject);
            _touchHandler.OnRightDown
                .Subscribe(eventData => OnEvent(eventData, TouchHandler.EventType.RightDown))
                .AddTo(gameObject);
            _touchHandler.OnClick
                .Subscribe(eventData => OnEvent(eventData, TouchHandler.EventType.Click))
                .AddTo(gameObject);
            _touchHandler.OnDoubleClick
                .Subscribe(eventData => OnEvent(eventData, TouchHandler.EventType.DoubleClick))
                .AddTo(gameObject);
            _touchHandler.OnMultipleClick
                .Subscribe(eventData => OnEvent(eventData, TouchHandler.EventType.MultipleClick))
                .AddTo(gameObject);
            _touchHandler.OnMiddleClick
                .Subscribe(eventData => OnEvent(eventData, TouchHandler.EventType.MiddleClick))
                .AddTo(gameObject);
            _touchHandler.OnRightClick
                .Subscribe(eventData => OnEvent(eventData, TouchHandler.EventType.RightClick))
                .AddTo(gameObject);
            _touchHandler.OnExit
                .Subscribe(eventData => OnEvent(eventData, TouchHandler.EventType.Exit))
                .AddTo(gameObject);
        }

        private void OnEvent(PointerEventData eventData, TouchHandler.EventType eventType)
        {
            // Debug.Log($"[TouchHandlerEventChannel] OnEvent({eventData}, {eventType})");
            foreach (var eventSetting in _eventSettings)
            {
                if (eventSetting.When != eventType)
                {
                    continue;
                }

                eventSetting.Invoke.Invoke(eventData);
            }
        }
    }
}
