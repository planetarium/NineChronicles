using System;
using UnityEngine;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.AnimatedGraphics;
using Nekoyume.UI.Tween;

namespace Nekoyume.UI.Module.Lobby
{
    public enum MenuType
    {
        Combination,
        Ranking,
        Shop,
        Quest,
        Mimisbrunnr,
        Staking,
        Worldboss,
    }

    public class MainMenu : MonoBehaviour
    {
        public string localizationKey = string.Empty;
        public MenuType type;

        [SerializeField]
        private SpeechBubble speechBubble;

        [SerializeField]
        private GameObject[] lockObjects;

        [SerializeField]
        private GameObject[] unlockObjects;

        [SerializeField]
        private HoverScaleTweener hoverScaleTweener;

        private MessageCat _cat;

        private Vector3 _originLocalScale;
        private string _messageForCat;
        private int _requireStage;

        public bool IsUnlocked { get; private set; }

        #region Mono

        private void Awake()
        {
            if (!GetComponentInParent<Menu>())
                throw new NotFoundComponentException<Menu>();

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
                case MenuType.Mimisbrunnr:
                    _requireStage = GameConfig.RequireClearedStageLevel.UIBottomMenuMimisbrunnr;
                    break;
                case MenuType.Staking:
                    _requireStage = 1;  // always allow
                    break;
                case MenuType.Worldboss:
                    _requireStage = GameConfig.RequireClearedStageLevel.ActionsInRaid;
                    break;
                    ;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var unlockConditionString = string.Format(
                L10nManager.Localize("UI_STAGE_LOCK_FORMAT"),
                _requireStage);
            _messageForCat =
                $"{L10nManager.Localize(localizationKey)}\n<sprite name=\"UI_icon_lock_01\"> {unlockConditionString}";

            hoverScaleTweener.AddCondition(PointEnterTrigger, PointExitTrigger);
        }

        private bool PointEnterTrigger()
        {
            if (!IsUnlocked)
            {
                if (_cat)
                {
                    _cat.Hide();
                }

                _cat = Widget.Find<MessageCatTooltip>()
                    .Show(true, _messageForCat, gameObject);
            }

            return IsUnlocked;
        }

        private bool PointExitTrigger()
        {
            if (IsUnlocked)
            {
                ResetLocalizationKey();
            }
            else
            {
                if (!_cat)
                {
                    return IsUnlocked;
                }

                _cat.Hide();
                _cat = null;
            }

            return IsUnlocked;
        }

        #endregion

        public void JingleTheCat()
        {
            if (!_cat)
            {
                return;
            }

            _cat.Jingle();
        }

        private void ResetLocalizationKey()
        {
            if (!speechBubble)
            {
                return;
            }

            speechBubble.ResetKey();
            speechBubble.Hide();
        }

        public void Update()
        {
            if (_requireStage > 0)
            {
                if (States.Instance.CurrentAvatarState.worldInformation != null &&
                    States.Instance.CurrentAvatarState.worldInformation
                        .TryGetUnlockedWorldByStageClearedBlockIndex(out var world))
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

            foreach (var go in lockObjects)
            {
                go.SetActive(!IsUnlocked);
            }

            foreach (var go in unlockObjects)
            {
                go.SetActive(IsUnlocked);
            }

            gameObject.SetActive(true);
            speechBubble.Init();
        }
    }
}
