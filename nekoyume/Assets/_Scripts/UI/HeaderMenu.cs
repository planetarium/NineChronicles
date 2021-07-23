using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.EnumType;
using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.State;
using Nekoyume.UI.AnimatedGraphics;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class HeaderMenu : Widget
    {
        private enum ToggleType
        {
            Quest,
            AvatarInfo,
            CombinationSlots,
            Mail,
            Rank,
            Chat,
            Settings,
            Quit,
        }

        [Serializable]
        private class ToggleInfo
        {
            public ToggleType Type;
            public Toggle Toggle;
            public Image Notification;
        }

        public override WidgetType WidgetType => WidgetType.Popup;

        // 네비게이션 버튼.
        public ToggleableButton quitButton;
        public GlowingButton exitButton;

        // 코드 보상 버튼
        public CodeRewardButton codeRewardButton;

        public CanvasGroup canvasGroup;
        public VFX inventoryVFX;
        public VFX workshopVFX;

        private Animator _inventoryAnimator;
        private MessageCat _cat;

        [SerializeField] private List<ToggleInfo> toggles = new List<ToggleInfo>();

        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();
        private readonly Dictionary<ToggleType, Widget> _toggleWidgets = new Dictionary<ToggleType, Widget>();
        private readonly Dictionary<ToggleType, ReactiveProperty<bool>> _toggleNotifications =
            new Dictionary<ToggleType, ReactiveProperty<bool>>()
            {
                {ToggleType.Quest, new ReactiveProperty<bool>(false)},
                {ToggleType.AvatarInfo, new ReactiveProperty<bool>(false)},
                {ToggleType.CombinationSlots, new ReactiveProperty<bool>(false)},
                {ToggleType.Mail, new ReactiveProperty<bool>(false)},
                {ToggleType.Rank, new ReactiveProperty<bool>(false)},
            };

        private readonly Dictionary<ToggleType, int> _toggleUnlockStages =
            new Dictionary<ToggleType, int>()
            {
                {ToggleType.Quest, GameConfig.RequireClearedStageLevel.UIBottomMenuQuest},
                {ToggleType.AvatarInfo, GameConfig.RequireClearedStageLevel.UIBottomMenuCharacter},
                {ToggleType.CombinationSlots, GameConfig.RequireClearedStageLevel.CombinationEquipmentAction},
                {ToggleType.Mail, GameConfig.RequireClearedStageLevel.UIBottomMenuMail},
                {ToggleType.Rank, 1},
                {ToggleType.Chat, GameConfig.RequireClearedStageLevel.UIBottomMenuChat},
                {ToggleType.Settings, 1},
                {ToggleType.Quit, 1},
            };

        private long _blockIndex;

        protected override void Awake()
        {
            base.Awake();

            _toggleWidgets.Add(ToggleType.Quest, Find<Quest>());
            _toggleWidgets.Add(ToggleType.AvatarInfo, Find<AvatarInfo>());
            _toggleWidgets.Add(ToggleType.CombinationSlots, Find<CombinationSlots>());
            _toggleWidgets.Add(ToggleType.Mail, Find<Mail>());
            _toggleWidgets.Add(ToggleType.Rank, Find<Rank>());
            _toggleWidgets.Add(ToggleType.Settings, Find<Settings>());
            _toggleWidgets.Add(ToggleType.Chat, Find<Confirm>());
            _toggleWidgets.Add(ToggleType.Quit, Find<QuitPopup>());

            foreach (var toggleInfo in toggles)
            {
                if (_toggleNotifications.ContainsKey(toggleInfo.Type))
                {
                    _toggleNotifications[toggleInfo.Type].SubscribeTo(toggleInfo.Notification)
                        .AddTo(gameObject);
                }

                toggleInfo.Toggle.onValueChanged.AddListener((value) =>
                {
                    var widget = _toggleWidgets[toggleInfo.Type];
                    if (value)
                    {
                        var requiredStage = _toggleUnlockStages[toggleInfo.Type];
                        if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(requiredStage))
                        {
                            var msg = string.Format(L10nManager.Localize("UI_STAGE_LOCK_FORMAT"), requiredStage);
                            OneLinePopup.Push(MailType.System, msg);
                            toggleInfo.Toggle.isOn = false;
                            return;
                        }

                        widget.Show();
                    }
                    else
                    {
                        if (widget.isActiveAndEnabled)
                        {
                            widget.Close();
                        }
                    }
                });
            }

            // // worldMapButton.SetWidgetType<WorldMapPaper>();
            //
            // chatButton.OnClick
            //     .Subscribe(SubScribeOnClickChat)
            //     .AddTo(gameObject);
            //

            //
            // SubmitWidget = null;
            // CloseWidget = null;

        }

        public override void Initialize()
        {
            base.Initialize();

            Game.Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(SubscribeBlockIndex)
                .AddTo(gameObject);
        }

        private static void SubScribeOnClickChat(ToggleableButton button)
        {
            var confirm = Find<Confirm>();
            confirm.CloseCallback = result =>
            {
                if (result == ConfirmResult.No)
                {
                    return;
                }

                Application.OpenURL(GameConfig.DiscordLink);
            };
            confirm.Set("UI_PROCEED_DISCORD", "UI_PROCEED_DISCORD_CONTENT", blurRadius: 2);
            HelpPopup.HelpMe(100012, true);
        }

        private static void SubscribeOnClick(ToggleableButton button)
        {
            Find<Alert>().Set(
                "UI_ALERT_NOT_IMPLEMENTED_TITLE",
                "UI_ALERT_NOT_IMPLEMENTED_CONTENT",
                blurRadius: 2);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _disposablesAtOnEnable.DisposeAllAndClear();
            ReactiveAvatarState.QuestList?.Subscribe(SubscribeAvatarQuestList).AddTo(_disposablesAtOnEnable);
            ReactiveAvatarState.MailBox?.Subscribe(SubscribeAvatarMailBox).AddTo(_disposablesAtOnEnable);
            ReactiveAvatarState.Inventory?.Subscribe(SubscribeInventory).AddTo(_disposablesAtOnEnable);
            ReactiveAvatarState.LevelUp.Subscribe(SubscribeUnlockState)
                .AddTo(_disposablesAtOnEnable);

        }

        public void SubscribeUnlockState(int level)
        {
            // CheckMailUnlock();
        }

        protected override void OnDisable()
        {
            _disposablesAtOnEnable.DisposeAllAndClear();
            base.OnDisable();
        }

        // public void SetIntractable(bool intractable)
        // {
        //     canvasGroup.interactable = intractable;
        // }
        //
        // public void PlayGetItemAnimation()
        // {
        //     characterButton.Animator.Play("GetItem");
        //     inventoryVFX.Play();
        // }
        //
        // public void PlayWorkShopVFX()
        // {
        //     combinationButton.Animator.Play("GetItem");
        //     workshopVFX.Play();
        // }


        private void SubscribeBlockIndex(long blockIndex)
        {
            _blockIndex = blockIndex;
            UpdateCombinationNotification();

            var mailBox = Find<Mail>().MailBox;
            if (mailBox is null)
            {
                return;
            }

            _toggleNotifications[ToggleType.Mail].Value =
                mailBox.Any(i => i.New && i.requiredBlockIndex <= _blockIndex);;
        }

        private void SubscribeAvatarMailBox(MailBox mailBox)
        {
            if (mailBox is null)
            {
                Debug.LogWarning($"{nameof(mailBox)} is null.");
                return;
            }

            _toggleNotifications[ToggleType.Mail].Value =
                mailBox.Any(i => i.New && i.requiredBlockIndex <= _blockIndex);
        }

        private void SubscribeAvatarQuestList(QuestList questList)
        {
            if (questList is null)
            {
                Debug.LogWarning($"{nameof(questList)} is null.");
                return;
            }

            var hasNotification = questList.Any(quest => quest.IsPaidInAction && quest.isReceivable);
            _toggleNotifications[ToggleType.Quest].Value = hasNotification;
            Find<Quest>().SetList(questList);
        }

        private void SubscribeInventory(Nekoyume.Model.Item.Inventory inventory)
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var avatarLevel = States.Instance.CurrentAvatarState?.level ?? 0;
            var hasNotification = inventory?.HasNotification(avatarLevel, blockIndex) ?? false;
            UpdateInventoryNotification(hasNotification);
        }

        // #region show button
        //
        // private bool ShowButton(ToggleableType toggleableType)
        // {
        //     switch (toggleableType)
        //     {
        //         case ToggleableType.Mail:
        //             return ShowMailButton();
        //         case ToggleableType.Quest:
        //             return ShowQuestButton();
        //         case ToggleableType.Chat:
        //             return ShowChatButton();
        //         case ToggleableType.Ranking:
        //             return ShowRankingButton();
        //         case ToggleableType.Character:
        //             return ShowCharacterButton();
        //         case ToggleableType.WorldMap:
        //             return ShowWorldMapButton();
        //         case ToggleableType.Settings:
        //             return ShowSettingsButton();
        //         case ToggleableType.Combination:
        //             return ShowCombinationButton();
        //         default:
        //             throw new ArgumentOutOfRangeException(nameof(toggleableType), toggleableType,
        //                 null);
        //     }
        // }
        //
        // private bool ShowChatButton()
        // {
        //     chatButton.Show();
        //
        //     var requiredStage = GameConfig.RequireClearedStageLevel.UIBottomMenuChat;
        //     if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(GameConfig
        //         .RequireClearedStageLevel.UIBottomMenuChat))
        //     {
        //         if (_disposablesForLockedButtons.TryGetValue(ToggleableType.Chat, out var _))
        //         {
        //             chatButton.SetInteractable(false);
        //             return true;
        //         }
        //
        //         (IDisposable enter, IDisposable exit) disposables;
        //
        //         disposables.enter = chatButton.onPointerEnter.Subscribe(_ =>
        //         {
        //             if (_cat)
        //             {
        //                 _cat.Hide();
        //             }
        //
        //             var unlockConditionString = string.Format(
        //                 L10nManager.Localize("UI_STAGE_LOCK_FORMAT"),
        //                 requiredStage);
        //
        //             var message =
        //                 $"{L10nManager.Localize(chatButton.localizationKey)}\n<sprite name=\"UI_icon_lock_01\"> {unlockConditionString}";
        //             _cat = Find<MessageCatManager>().Show(true, message);
        //         }).AddTo(chatButton.gameObject);
        //         disposables.exit = chatButton.onPointerExit.Subscribe(_ =>
        //         {
        //             if (_cat)
        //             {
        //                 _cat.Hide();
        //             }
        //         }).AddTo(chatButton.gameObject);
        //         _disposablesForLockedButtons[ToggleableType.Chat] = disposables;
        //         chatButton.SetInteractable(false);
        //     }
        //     else
        //     {
        //         if (_disposablesForLockedButtons.TryGetValue(ToggleableType.Chat, out var tuple))
        //         {
        //             tuple.pointerEnter.Dispose();
        //             tuple.pointerExit.Dispose();
        //             _disposablesForLockedButtons.Remove(ToggleableType.Chat);
        //         }
        //         chatButton.SetInteractable(true);
        //     }
        //
        //     return true;
        // }
        //
        // private bool CheckMailUnlock()
        // {
        //     var requiredStage = GameConfig.RequireClearedStageLevel.UIBottomMenuMail;
        //     if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(GameConfig.RequireClearedStageLevel.UIBottomMenuMail))
        //     {
        //         (IDisposable enter, IDisposable exit) disposables;
        //
        //         disposables.enter = mailButton.onPointerEnter.Subscribe(_ =>
        //         {
        //             if (_cat)
        //             {
        //                 _cat.Hide();
        //             }
        //
        //             var unlockConditionString = string.Format(
        //                 L10nManager.Localize("UI_STAGE_LOCK_FORMAT"),
        //                 requiredStage);
        //             var message =
        //                 $"{L10nManager.Localize(mailButton.localizationKey)}\n<sprite name=\"UI_icon_lock_01\"> {unlockConditionString}";
        //             _cat = Find<MessageCatManager>().Show(true, message, true);
        //         }).AddTo(mailButton.gameObject);
        //
        //
        //         disposables.exit = mailButton.onPointerExit.Subscribe(_ =>
        //         {
        //             if (_cat)
        //             {
        //                 _cat.Hide();
        //             }
        //         }).AddTo(mailButton.gameObject);
        //         _disposablesForLockedButtons[ToggleableType.Mail] = disposables;
        //         mailButton.SetInteractable(false);
        //     }
        //     else
        //     {
        //         if (_disposablesForLockedButtons.TryGetValue(ToggleableType.Mail, out var tuple))
        //         {
        //             tuple.pointerEnter.Dispose();
        //             tuple.pointerExit.Dispose();
        //             _disposablesForLockedButtons.Remove(ToggleableType.Mail);
        //         }
        //         mailButton.SetInteractable(true);
        //     }
        //     return true;
        // }
        //
        // private bool ShowQuestButton()
        // {
        //     questButton.Show();
        //
        //     var requiredStage = GameConfig.RequireClearedStageLevel.UIBottomMenuQuest;
        //     if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(GameConfig
        //         .RequireClearedStageLevel.UIBottomMenuQuest))
        //     {
        //         if (_disposablesForLockedButtons.TryGetValue(ToggleableType.Quest, out var _))
        //         {
        //             questButton.SetInteractable(false);
        //             return true;
        //         }
        //
        //         (IDisposable enter, IDisposable exit) disposables;
        //
        //         disposables.enter = questButton.onPointerEnter.Subscribe(_ =>
        //         {
        //             if (_cat)
        //             {
        //                 _cat.Hide();
        //             }
        //
        //             var unlockConditionString = string.Format(
        //                 L10nManager.Localize("UI_STAGE_LOCK_FORMAT"),
        //                 requiredStage);
        //             var message =
        //                 $"{L10nManager.Localize(questButton.localizationKey)}\n<sprite name=\"UI_icon_lock_01\"> {unlockConditionString}";
        //             _cat = Find<MessageCatManager>().Show(true, message, true);
        //         }).AddTo(questButton.gameObject);
        //         disposables.exit = questButton.onPointerExit.Subscribe(_ =>
        //         {
        //             if (_cat)
        //             {
        //                 _cat.Hide();
        //             }
        //         }).AddTo(questButton.gameObject);
        //         _disposablesForLockedButtons[ToggleableType.Quest] = disposables;
        //         questButton.SetInteractable(false);
        //     }
        //     else
        //     {
        //         if (_disposablesForLockedButtons.TryGetValue(ToggleableType.Quest, out var tuple))
        //         {
        //             tuple.pointerEnter.Dispose();
        //             tuple.pointerExit.Dispose();
        //             _disposablesForLockedButtons.Remove(ToggleableType.Quest);
        //         }
        //         questButton.SetInteractable(true);
        //     }
        //
        //     return true;
        // }
        //
        // private bool ShowRankingButton()
        // {
        //     rankingButton.Show();
        //
        //     var requiredStage = 1;
        //     if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(requiredStage))
        //     {
        //         if (_disposablesForLockedButtons.TryGetValue(ToggleableType.Ranking, out var _))
        //         {
        //             rankingButton.SetInteractable(false);
        //             return true;
        //         }
        //
        //         (IDisposable enter, IDisposable exit) disposables;
        //
        //         disposables.enter = rankingButton.onPointerEnter.Subscribe(_ =>
        //         {
        //             if (_cat)
        //             {
        //                 _cat.Hide();
        //             }
        //
        //             var unlockConditionString = string.Format(
        //                 L10nManager.Localize("UI_STAGE_LOCK_FORMAT"),
        //                 requiredStage);
        //             var message =
        //                 $"{L10nManager.Localize(rankingButton.localizationKey)}\n<sprite name=\"UI_icon_lock_01\"> {unlockConditionString}";
        //             _cat = Find<MessageCatManager>().Show(true, message, true);
        //         }).AddTo(rankingButton.gameObject);
        //         disposables.exit = rankingButton.onPointerExit.Subscribe(_ =>
        //         {
        //             if (_cat)
        //             {
        //                 _cat.Hide();
        //             }
        //         }).AddTo(rankingButton.gameObject);
        //         _disposablesForLockedButtons[ToggleableType.Ranking] = disposables;
        //         rankingButton.SetInteractable(false);
        //     }
        //     else
        //     {
        //         if (_disposablesForLockedButtons.TryGetValue(ToggleableType.Ranking, out var tuple))
        //         {
        //             tuple.pointerEnter.Dispose();
        //             tuple.pointerExit.Dispose();
        //             _disposablesForLockedButtons.Remove(ToggleableType.Ranking);
        //         }
        //         rankingButton.SetInteractable(true);
        //     }
        //
        //     return true;
        // }
        //
        // private bool ShowCharacterButton()
        // {
        //     characterButton.Show();
        //
        //     var requiredStage = GameConfig.RequireClearedStageLevel.UIBottomMenuCharacter;
        //     if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(GameConfig
        //         .RequireClearedStageLevel.UIBottomMenuCharacter))
        //     {
        //         if (_disposablesForLockedButtons.TryGetValue(ToggleableType.Character, out var _))
        //         {
        //             characterButton.SetInteractable(false);
        //             return true;
        //         }
        //
        //         (IDisposable enter, IDisposable exit) disposables;
        //
        //         disposables.enter = characterButton.onPointerEnter.Subscribe(_ =>
        //         {
        //             if (_cat)
        //             {
        //                 _cat.Hide();
        //             }
        //
        //             var unlockConditionString = string.Format(
        //                 L10nManager.Localize("UI_STAGE_LOCK_FORMAT"),
        //                 requiredStage);
        //             var message =
        //                 $"{L10nManager.Localize(characterButton.localizationKey)}\n<sprite name=\"UI_icon_lock_01\"> {unlockConditionString}";
        //             _cat = Find<MessageCatManager>().Show(true, message, true);
        //         }).AddTo(characterButton.gameObject);
        //         disposables.exit = characterButton.onPointerExit.Subscribe(_ =>
        //         {
        //             if (_cat)
        //             {
        //                 _cat.Hide();
        //             }
        //         }).AddTo(characterButton.gameObject);
        //         _disposablesForLockedButtons[ToggleableType.Character] = disposables;
        //         characterButton.SetInteractable(false);
        //     }
        //     else
        //     {
        //         if (_disposablesForLockedButtons.TryGetValue(ToggleableType.Character, out var tuple))
        //         {
        //             tuple.pointerEnter.Dispose();
        //             tuple.pointerExit.Dispose();
        //             _disposablesForLockedButtons.Remove(ToggleableType.Character);
        //         }
        //         characterButton.SetInteractable(true);
        //     }
        //
        //     return true;
        // }
        //
        // private bool ShowWorldMapButton()
        // {
        //     worldMapButton.Show();
        //     return true;
        // }
        //
        // private bool ShowSettingsButton()
        // {
        //     settingsButton.Show();
        //
        //     if (_disposablesForLockedButtons.TryGetValue(ToggleableType.Settings, out var tuple))
        //         {
        //             tuple.pointerEnter.Dispose();
        //             tuple.pointerExit.Dispose();
        //             _disposablesForLockedButtons.Remove(ToggleableType.Settings);
        //         }
        //         settingsButton.SetInteractable(true);
        //
        //     return true;
        // }
        //
        // private bool ShowCombinationButton()
        // {
        //     combinationButton.Show();
        //
        //     var requiredStage = GameConfig.RequireClearedStageLevel.CombinationEquipmentAction;
        //     if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(GameConfig
        //         .RequireClearedStageLevel.CombinationEquipmentAction))
        //     {
        //         if (_disposablesForLockedButtons.TryGetValue(ToggleableType.Combination, out var _))
        //         {
        //             combinationButton.SetInteractable(false);
        //             return true;
        //         }
        //
        //         (IDisposable enter, IDisposable exit) disposables;
        //
        //         disposables.enter = combinationButton.onPointerEnter.Subscribe(_ =>
        //         {
        //             if (_cat)
        //             {
        //                 _cat.Hide();
        //             }
        //
        //             var unlockConditionString = string.Format(
        //                 L10nManager.Localize("UI_STAGE_LOCK_FORMAT"),
        //                 requiredStage);
        //             var message =
        //                 $"{L10nManager.Localize(combinationButton.localizationKey)}\n<sprite name=\"UI_icon_lock_01\"> {unlockConditionString}";
        //             _cat = Find<MessageCatManager>().Show(true, message);
        //         }).AddTo(combinationButton.gameObject);
        //         disposables.exit = combinationButton.onPointerExit.Subscribe(_ =>
        //         {
        //             if (_cat)
        //             {
        //                 _cat.Hide();
        //             }
        //         }).AddTo(combinationButton.gameObject);
        //         _disposablesForLockedButtons[ToggleableType.Combination] = disposables;
        //         combinationButton.SetInteractable(false);
        //     }
        //     else
        //     {
        //         if (_disposablesForLockedButtons.TryGetValue(ToggleableType.Combination, out var tuple))
        //         {
        //             tuple.pointerEnter.Dispose();
        //             tuple.pointerExit.Dispose();
        //             _disposablesForLockedButtons.Remove(ToggleableType.Combination);
        //         }
        //         combinationButton.SetInteractable(true);
        //     }
        //
        //     return true;
        // }
        //
        // #endregion
        //


        public void UpdateCombinationNotification()
        {
            var combinationSlots = Find<CombinationSlots>().slots;
            var hasNotification = combinationSlots.Any(slot => slot.HasNotification.Value);
            _toggleNotifications[ToggleType.CombinationSlots].Value = hasNotification;
        }

        public void UpdateInventoryNotification(bool hasNotification)
        {
            _toggleNotifications[ToggleType.AvatarInfo].Value = hasNotification;
        }

        public void TutorialActionClickBottomMenuWorkShopButton()
        {
            var info = toggles.FirstOrDefault(x => x.Type.Equals(ToggleType.CombinationSlots));
            if (info != null)
            {
                info.Toggle.isOn = true;
            }
        }

        public void TutorialActionClickBottomMenuMailButton()
        {
            var info = toggles.FirstOrDefault(x => x.Type.Equals(ToggleType.Mail));
            if (info != null)
            {
                info.Toggle.isOn = true;
            }
        }

        public void TutorialActionClickBottomMenuCharacterButton()
        {
            var info = toggles.FirstOrDefault(x => x.Type.Equals(ToggleType.AvatarInfo));
            if (info != null)
            {
                info.Toggle.isOn = true;
            }
        }
    }
}
