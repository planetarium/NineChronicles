using System;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using System.Collections.Generic;

namespace Nekoyume.UI.Module
{
    public class MainMenu : MonoBehaviour
    {
        public float TweenDuration = 0.3f;
        public float BgScale = 1.05f;
        public string BgName;
        public CanvasGroup Caption;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();

        #region Mono

        private void Awake()
        {
            Menu parent = GetComponentInParent<Menu>();
            if (!parent)
                throw new NotFoundComponentException<Menu>();

            gameObject.AddComponent<ObservablePointerClickTrigger>()
                .OnPointerClickAsObservable()
                .Subscribe(x => {
                    parent.Stage.background.transform.Find(BgName)?.DOScale(1.0f, 0.0f);
                    Caption?.DOFade(0.0f, 0.0f);
                })
                .AddTo(_disposablesForAwake);

            gameObject.AddComponent<ObservablePointerEnterTrigger>()
                .OnPointerEnterAsObservable()
                .Subscribe(x => {
                    parent.Stage.background.transform.Find(BgName)?.DOScale(BgScale, TweenDuration);
                    Caption?.DOFade(1.0f, 0.2f);
                })
                .AddTo(_disposablesForAwake);

            gameObject.AddComponent<ObservablePointerExitTrigger>()
                .OnPointerExitAsObservable()
                .Subscribe(x => {
                    parent.Stage.background.transform.Find(BgName)?.DOScale(1.0f, TweenDuration);
                    Caption?.DOFade(0.0f, 0.2f);
                })
                .AddTo(_disposablesForAwake); ;
        }

        private void Start()
        {
            Caption.alpha = 0.0f;
        }

        private void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
        }

        #endregion
    }
}
