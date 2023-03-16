using DG.Tweening;
using UnityEngine;
using System;

namespace Nekoyume.UI.Tween
{
    using Bencodex.Types;
    using System.Collections.Generic;
    using UniRx;
    using UniRx.Triggers;
    using UnityEngine.EventSystems;

    public class HoverScaleTweener : MonoBehaviour
    {
        [SerializeField]
        private RectTransform target;

        private ObservablePointerEnterTrigger _enterTrigger;
        private ObservablePointerExitTrigger _exitTrigger;
        private Func<bool> _onPointerEnter;
        private Func<bool> _onPointerExit;

        [SerializeField]
        private float tweenDuration = 0.3f;

        [SerializeField]
        private float targetScale = 1.05f;

        private Vector3 _originLocalScale;
        private readonly List<IDisposable> _disposables = new();

        private void Awake()
        {
            _originLocalScale = target.localScale;
            _enterTrigger = gameObject.AddComponent<ObservablePointerEnterTrigger>();
            _exitTrigger = gameObject.AddComponent<ObservablePointerExitTrigger>();
        }

        private void OnEnable()
        {
            target.localScale = _originLocalScale;
            _enterTrigger.OnPointerEnterAsObservable()
                .Subscribe(OnPointerEnter)
                .AddTo(_disposables);
            _exitTrigger.OnPointerExitAsObservable()
                .Subscribe(OnPointerExit)
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        public void AddCondition(System.Func<bool> enter = null, System.Func<bool> exit = null)
        {
            _onPointerEnter = enter;
            _onPointerExit = exit;
        }

        private void OnPointerEnter(PointerEventData eventData)
        {
            target.DOScale(_originLocalScale * targetScale, tweenDuration);
            _onPointerEnter?.Invoke();
        }

        private void OnPointerExit(PointerEventData eventData)
        {
            target.DOScale(_originLocalScale, tweenDuration);
            _onPointerExit?.Invoke();
        }
    }
}
