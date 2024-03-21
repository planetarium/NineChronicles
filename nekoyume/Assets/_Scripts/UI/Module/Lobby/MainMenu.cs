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
        WorldBoss,
        Dcc,
        PatrolReward,
        SeasonPass,
        Collection,
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
        protected int _requireStage;
        private const int TutorialEndStage = 10;

        public bool IsUnlocked { get; private set; }

        #region Mono

        protected virtual void Awake()
        {
            if (!GetComponentInParent<Menu>())
                throw new NotFoundComponentException<Menu>();

            switch (type)
            {
                case MenuType.Combination:
                    _requireStage = Game.LiveAsset.GameConfig.RequiredStage.WorkShop;
                    break;
                case MenuType.Ranking:
                    _requireStage = Game.LiveAsset.GameConfig.RequiredStage.Arena;
                    break;
                case MenuType.Shop:
                    _requireStage = Platform.IsMobilePlatform()
                        ? 1
                        : Game.LiveAsset.GameConfig.RequiredStage.Shop;
                    break;
                case MenuType.Quest:
                    _requireStage = Game.LiveAsset.GameConfig.RequiredStage.Adventure;
                    break;
                case MenuType.Mimisbrunnr:
                    _requireStage = Game.LiveAsset.GameConfig.RequiredStage.Mimisbrunnr;
                    break;
                case MenuType.WorldBoss:
                    _requireStage = Game.LiveAsset.GameConfig.RequiredStage.WorldBoss;
                    break;
                case MenuType.SeasonPass:
                    _requireStage = Game.LiveAsset.GameConfig.RequiredStage.SeasonPass;
                    break;
                // always allow
                case MenuType.Staking:
                case MenuType.Dcc:
                case MenuType.PatrolReward:
                case MenuType.Collection:
                    _requireStage = Game.LiveAsset.GameConfig.RequiredStage.TutorialEnd;
                    break;
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
            return IsUnlocked;
        }

        private bool PointExitTrigger()
        {
            if (IsUnlocked)
            {
                ResetLocalizationKey();
            }

            return IsUnlocked;
        }

        #endregion

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
