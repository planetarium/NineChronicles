using DG.Tweening;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    using UniRx;
    using UniRx.Triggers;

    public class HoverScaleTweener : MonoBehaviour
    {
        [SerializeField]
        private RectTransform target;

        [SerializeField]
        private float tweenDuration = 0.3f;

        [SerializeField]
        private float targetScale = 1.05f;

        private Vector3 _originLocalScale;
        private bool _isAddedCondition;

        private void Awake()
        {
            _originLocalScale = target.localScale;
        }

        private void Start()
        {
            if (!_isAddedCondition)
            {
                AddCondition();
            }
        }

        private void OnEnable()
        {
            target.localScale = _originLocalScale;
        }

        public void AddCondition(System.Func<bool> enter = null, System.Func<bool> exit = null)
        {
            gameObject.AddComponent<ObservablePointerEnterTrigger>()
                .OnPointerEnterAsObservable().Subscribe(x =>
                {
                    if (enter == null)
                    {
                        target.DOScale(_originLocalScale * targetScale, tweenDuration);
                        return;
                    }

                    if (enter.Invoke())
                    {
                        target.DOScale(_originLocalScale * targetScale, tweenDuration);
                    }
                })
                .AddTo(gameObject);

            gameObject.AddComponent<ObservablePointerExitTrigger>()
                .OnPointerExitAsObservable().Subscribe(x =>
                {
                    if (exit == null)
                    {
                        target.DOScale(_originLocalScale, tweenDuration);
                        return;
                    }

                    if (exit.Invoke())
                    {
                        target.DOScale(_originLocalScale, tweenDuration);
                    }
                })
                .AddTo(gameObject);

            _isAddedCondition = true;
        }
    }
}
