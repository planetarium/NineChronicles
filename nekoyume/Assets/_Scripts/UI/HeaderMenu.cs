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

        public override WidgetType WidgetType => WidgetType.Screen;

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

        private ReactiveProperty<bool> test = new ReactiveProperty<bool>();
        private Subject<bool> _FocusSubject;

        protected override void Awake()
        {
            base.Awake();

            _toggleWidgets.Add(ToggleType.Quest, Find<Quest>());
            _toggleWidgets.Add(ToggleType.AvatarInfo, Find<AvatarInfo>());
            _toggleWidgets.Add(ToggleType.CombinationSlots, Find<CombinationSlots>());
            _toggleWidgets.Add(ToggleType.Mail, Find<Mail>());
            _toggleWidgets.Add(ToggleType.Rank, Find<Rank>());
            _toggleWidgets.Add(ToggleType.Settings, Find<Settings>());
            _toggleWidgets.Add(ToggleType.Chat, Find<ChatPopup>());
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

                        widget.Show(() =>
                        {
                            toggleInfo.Toggle.isOn = false;
                        });
                    }
                    else
                    {
                        if (widget.isActiveAndEnabled)
                        {
                            widget.Close(true);
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

        // private static void SubScribeOnClickChat(ToggleableButton button)
        // {
        //     var confirm = Find<Confirm>();
        //     confirm.CloseCallback = result =>
        //     {
        //         if (result == ConfirmResult.No)
        //         {
        //             return;
        //         }
        //
        //         Application.OpenURL(GameConfig.DiscordLink);
        //     };
        //     confirm.Set("UI_PROCEED_DISCORD", "UI_PROCEED_DISCORD_CONTENT", blurRadius: 2);
        //     HelpPopup.HelpMe(100012, true);
        // }

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
