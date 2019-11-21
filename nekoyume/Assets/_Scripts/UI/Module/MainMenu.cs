using System;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Character;

namespace Nekoyume.UI.Module
{
    public class MainMenu : MonoBehaviour
    {
        public float TweenDuration = 0.3f;
        public float BgScale = 1.05f;
        public SpeechBubble speechBubble;
        public string pointerEnterKey;
        public string pointerClickKey;
        public Npc npc;
        public Transform bgTransform;
        private string _defaultKey;
        private Vector3 _scaler;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();

        #region Mono

        private void Awake()
        {
            if (!GetComponentInParent<Menu>())
                throw new NotFoundComponentException<Menu>();

            if (speechBubble)
            {
                _defaultKey = speechBubble.localizationKey;
            }

            _scaler = bgTransform.localScale;

            gameObject.AddComponent<ObservablePointerClickTrigger>()
                .OnPointerClickAsObservable()
                .Subscribe(x => {
                    bgTransform.DOScale(_scaler * 1.0f, 0.0f);
                })
                .AddTo(_disposablesForAwake);

            gameObject.AddComponent<ObservablePointerEnterTrigger>()
                .OnPointerEnterAsObservable()
                .Subscribe(x => {
                    bgTransform.DOScale(_scaler * BgScale, TweenDuration);
                    ShowSpeech(pointerEnterKey);
                })
                .AddTo(_disposablesForAwake);

            gameObject.AddComponent<ObservablePointerExitTrigger>()
                .OnPointerExitAsObservable()
                .Subscribe(x => {
                    bgTransform.DOScale(_scaler * 1.0f, TweenDuration);
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
            if (npc)
            {
                npc.Emotion();
            }
        }

        private void ResetLocalizationKey()
        {
            if (speechBubble)
            {
                speechBubble.SetKey(_defaultKey);
                speechBubble.Hide();
            }
        }

        public void ShowRequiredLevelSpeech(int level)
        {
            speechBubble.SetKey(pointerClickKey);
            var format =
                LocalizationManager.Localize(
                    $"{pointerClickKey}{UnityEngine.Random.Range(0, speechBubble.SpeechCount)}");
            var speech = string.Format(format, level);
            StartCoroutine(speechBubble.CoShowText(speech));
            if (npc)
            {
                npc.Emotion();
            }
        }
    }
}
