using System;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using Assets.SimpleLocalization;
using Nekoyume.Game.Character;
using Nekoyume.State;
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
        public Image hasNotificationImage;
        public GameObject[] lockObjects;
        public GameObject[] unLockObjects;

        private int _requireStage;
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
                    _requireStage = GameConfig.RequireClearedStageLevel.UIMainMenuCombination;
                    break;
                case MenuType.Ranking:
                    _requireStage = GameConfig.RequireClearedStageLevel.UIMainMenuRankingBoard;
                    break;
                case MenuType.Shop:
                    _requireStage = GameConfig.RequireClearedStageLevel.UIMainMenuShop;
                    break;
                case MenuType.Quest:
                    _requireStage = GameConfig.RequireClearedStageLevel.UIMainMenuStage;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var unlockConditionString = string.Format(
                LocalizationManager.Localize("UI_STAGE_LOCK_FORMAT"),
                _requireStage);
            _messageForCat =
                $"{LocalizationManager.Localize(localizationKey)}\n<sprite name=\"UI_icon_lock_01\"> {unlockConditionString}";

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
                .AddTo(gameObject);

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
                .AddTo(gameObject);
        }

        private void OnEnable()
        {
            bgTransform.localScale = _originLocalScale;
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

        public void Update()
        {
            if (_requireStage > 0)
            {
                if (States.Instance.CurrentAvatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(
                    out var world))
                {
                    IsUnlocked = _requireStage <= world.StageClearedId;
                }
                else
                {
                    IsUnlocked = false;
                }
            }
            else
            {
                IsUnlocked = true;
            }

            if (npc)
            {
                npc.gameObject.SetActive(true);
            }

            foreach (var go in lockObjects)
            {
                go.SetActive(false);
            }

            foreach (var go in unLockObjects)
            {
                go.SetActive(true);
            }

            gameObject.SetActive(true);
            speechBubble.Init(true);
        }
    }
}
