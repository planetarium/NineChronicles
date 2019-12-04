using System;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Character;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public enum MenuType
    {
        Combination,
        Ranking,
        Shop,
        Quest,
    }
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
        public MenuType type;
        public GameObject[] lockObjects;
        public GameObject[] unLockObjects;
        private bool _unlock;

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
        public void Set(Player player)
        {
            var requiredLevel = 1;
            switch (type)
            {
                case MenuType.Combination:
                    requiredLevel = GameConfig.CombinationRequiredLevel;
                    break;
                case MenuType.Ranking:
                    requiredLevel = GameConfig.RankingRequiredLevel;
                    break;
                case MenuType.Shop:
                    requiredLevel = GameConfig.ShopRequiredLevel;
                    break;
            }

            _unlock = player.Level >= requiredLevel;

            if (npc)
            {
                npc.gameObject.SetActive(_unlock);
            }

            foreach (var go in lockObjects)
            {
                go.SetActive(!_unlock);
            }

            foreach (var go in unLockObjects)
            {
                go.SetActive(_unlock);
            }

            gameObject.SetActive(true);
            speechBubble.Init(_unlock);
        }
    }
}
