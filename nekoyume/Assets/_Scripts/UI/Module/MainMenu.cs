using System;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using System.Collections.Generic;
using Assets.SimpleLocalization;

namespace Nekoyume.UI.Module
{
    public class MainMenu : MonoBehaviour
    {
        public float TweenDuration = 0.3f;
        public float BgScale = 1.05f;
        public string BgName;
        public SpeechBubble speechBubble;
        public string pointerEnterKey;
        public string pointerClickKey;
        private string _defaultKey;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();

        #region Mono

        private void Awake()
        {
            Menu parent = GetComponentInParent<Menu>();
            if (!parent)
                throw new NotFoundComponentException<Menu>();

            if (speechBubble)
            {
                _defaultKey = speechBubble.localizationKey;
            }

            gameObject.AddComponent<ObservablePointerClickTrigger>()
                .OnPointerClickAsObservable()
                .Subscribe(x => {
                    parent.Stage.background.transform.Find(BgName)?.DOScale(1.0f, 0.0f);
                    ShowSpeech(pointerClickKey);
                })
                .AddTo(_disposablesForAwake);

            gameObject.AddComponent<ObservablePointerEnterTrigger>()
                .OnPointerEnterAsObservable()
                .Subscribe(x => {
                    parent.Stage.background.transform.Find(BgName)?.DOScale(BgScale, TweenDuration);
                    ShowSpeech(pointerEnterKey);
                })
                .AddTo(_disposablesForAwake);

            gameObject.AddComponent<ObservablePointerExitTrigger>()
                .OnPointerExitAsObservable()
                .Subscribe(x => {
                    parent.Stage.background.transform.Find(BgName)?.DOScale(1.0f, TweenDuration);
                    ResetLocalizationKey();
                })
                .AddTo(_disposablesForAwake); ;
        }

        private void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
        }

        #endregion

        private void ShowSpeech(string key)
        {
            if (speechBubble is null)
                return;
            if (speechBubble.gameObject.activeSelf)
                return;
            speechBubble.SetKey(key);
            StartCoroutine(speechBubble.CoShowText());
        }

        private void ResetLocalizationKey()
        {
            speechBubble?.SetKey(_defaultKey);
            speechBubble?.Hide();
        }

        public void ShowRequiredLevelSpeech(int level)
        {
            speechBubble.SetKey(pointerClickKey);
            var format =
                LocalizationManager.Localize(
                    $"{pointerClickKey}{UnityEngine.Random.Range(0, speechBubble.SpeechCount)}");
            var speech = string.Format(format, level);
            StartCoroutine(speechBubble.CoShowText(speech));
        }
    }
}
