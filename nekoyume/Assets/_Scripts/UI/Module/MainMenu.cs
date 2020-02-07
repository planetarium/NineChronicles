using System;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Character;
using Nekoyume.UI.AnimatedGraphics;
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
        public string localizationKey = string.Empty;
        public float TweenDuration = 0.3f;
        public float BgScale = 1.05f;
        public SpeechBubble speechBubble;
        public string pointerClickKey;
        public NPC npc;
        public Transform bgTransform;
        private Vector3 _originLocalScale;
        public MenuType type;
        public GameObject[] lockObjects;
        public GameObject[] unLockObjects;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();

        private int _requiredLevel;
        private string _messageForCat;
        private MessageCat _cat;
        
        public bool IsUnlocked { get; private set; }

        #region Mono

        private void Awake()
        {
            if (!GetComponentInParent<Menu>())
                throw new NotFoundComponentException<Menu>();

            _originLocalScale = bgTransform.localScale;

            switch (type)
            {
                case MenuType.Combination:
                    _requiredLevel = GameConfig.RequireLevel.Combination;
                    break;
                case MenuType.Ranking:
                    _requiredLevel = GameConfig.RequireLevel.Ranking;
                    break;
                case MenuType.Shop:
                    _requiredLevel = GameConfig.RequireLevel.Shop;
                    break;
                case MenuType.Quest:
                    _requiredLevel = GameConfig.RequireLevel.Quest;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            _messageForCat = $"{LocalizationManager.Localize(localizationKey)}\n<sprite name=\"UI_icon_lock_01\"> LV.{_requiredLevel}";

            gameObject.AddComponent<ObservablePointerEnterTrigger>()
                .OnPointerEnterAsObservable()
                .Subscribe(x =>
                {
                    if (!IsUnlocked)
                    {
                        if (_cat)
                        {
                            _cat.Hide();
                        }

                        _cat = Widget.Find<MessageCatManager>().Show(true, _messageForCat);

                        return;
                    }

                    bgTransform.DOScale(_originLocalScale * BgScale, TweenDuration);
                })
                .AddTo(_disposablesForAwake);

            gameObject.AddComponent<ObservablePointerExitTrigger>()
                .OnPointerExitAsObservable()
                .Subscribe(x =>
                {
                    if (!IsUnlocked)
                    {
                        if (!_cat)
                            return;

                        _cat.Hide();
                        _cat = null;

                        return;
                    }

                    bgTransform.DOScale(_originLocalScale, TweenDuration);
                    ResetLocalizationKey();
                })
                .AddTo(_disposablesForAwake);
        }

        private void OnEnable()
        {
            bgTransform.localScale = _originLocalScale;
        }

        private void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
        }

        #endregion

        public void JingleTheCat()
        {
            if (!_cat)
                return;
            
            _cat.Jingle();
        }
        
        private void ResetLocalizationKey()
        {
            if (speechBubble)
            {
                speechBubble.ResetKey();
                speechBubble.Hide();
            }
        }
        
        public void Set(Player player)
        {
            IsUnlocked = player.Level >= _requiredLevel;

            if (npc)
            {
                npc.gameObject.SetActive(IsUnlocked);
            }

            foreach (var go in lockObjects)
            {
                go.SetActive(!IsUnlocked);
            }

            foreach (var go in unLockObjects)
            {
                go.SetActive(IsUnlocked);
            }

            gameObject.SetActive(true);
            speechBubble.Init(IsUnlocked);
        }
    }
}
