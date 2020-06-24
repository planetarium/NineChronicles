using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.VFX;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.State;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class BottomMenu : Widget
    {
        // todo: 네비게이션 버튼들도 토글 그룹에 들어갔으니 여기서도 다뤄야 하겠음..
        public enum ToggleableType
        {
            Mail,
            Quest,
            Chat,
            IllustratedBook,
            Character,
            WorldMap,
            Settings,
            Combination,
        }

        public class Model : IDisposable
        {
            public readonly ReactiveProperty<UINavigator.NavigationType> NavigationType =
                new ReactiveProperty<UINavigator.NavigationType>(UINavigator.NavigationType.Back);

            public Action<BottomMenu> NavigationAction;

            public void Dispose()
            {
                NavigationType.Dispose();
            }
        }

        private static readonly List<ToggleableType> ToggleableTypes =
            Enum.GetValues(typeof(ToggleableType)).Cast<ToggleableType>().ToList();

        // 네비게이션 버튼.
        // todo: 이놈들도 ToggleableButton으로 바꿔야 함..
        public NormalButton mainButton;
        public NormalButton backButton;

        // 토글 그룹과 버튼.
        private ToggleGroup _toggleGroup;

        public IToggleGroup ToggleGroup => _toggleGroup;

        // 네비게이션 버튼.
        public ToggleableButton quitButton;
        public GlowingButton exitButton;

        // 일반 버튼.
        public NotifiableButton chatButton;
        public NotifiableButton mailButton;
        public NotifiableButton questButton;
        public NotifiableButton illustratedBookButton;
        public NotifiableButton characterButton;
        public NotifiableButton worldMapButton;
        public NotifiableButton settingsButton;
        public NotifiableButton combinationButton;

        public CanvasGroup canvasGroup;
        public VFX inventoryVFX;

        private Animator _inventoryAnimator;
        private long _blockIndex;

        [SerializeField]
        private RectTransform _buttons = null;

        private float _buttonsPositionY;
        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();

        public readonly Model SharedModel = new Model();
        public readonly Subject<bool> HasNotificationInMail = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInQuest = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInChat = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInIllustratedBook = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInCharacter = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInWorldMap = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInSettings = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInCombination = new Subject<bool>();

        protected override WidgetType WidgetType => WidgetType.Popup;

        protected override void Awake()
        {
            base.Awake();

            backButton.OnClick
                .Subscribe(_ => SubscribeNavigationButtonClick())
                .AddTo(gameObject);
            mainButton.OnClick
                .Subscribe(_ => SubscribeNavigationButtonClick())
                .AddTo(gameObject);
            quitButton.OnClick
                .Subscribe(_ => SubscribeNavigationButtonClick())
                .AddTo(gameObject);
            exitButton.OnClick
                .Subscribe(_ => SubscribeNavigationButtonClick())
                .AddTo(gameObject);

            quitButton.SetWidgetType<Confirm>();
            exitButton.SetWidgetType<Confirm>();
            mailButton.SetWidgetType<Mail>();
            questButton.SetWidgetType<Quest>();
            characterButton.SetWidgetType<AvatarInfo>();
            settingsButton.SetWidgetType<Settings>();
            chatButton.SetWidgetType<Confirm>();
            combinationButton.SetWidgetType<CombinationSlots>();
            // todo: 지금 월드맵 띄우는 것을 위젯으로 빼고, 여기서 설정하기?
            // worldMapButton.SetWidgetType<WorldMapPaper>();

            chatButton.OnClick
                .Subscribe(SubScribeOnClickChat)
                .AddTo(gameObject);
            // 미구현
            illustratedBookButton.OnClick
                .Subscribe(SubscribeOnClick)
                .AddTo(gameObject);
            illustratedBookButton.SetWidgetType<Alert>();

            _toggleGroup = new ToggleGroup();
            _toggleGroup.RegisterToggleable(quitButton);
            _toggleGroup.RegisterToggleable(exitButton);
            _toggleGroup.RegisterToggleable(mailButton);
            _toggleGroup.RegisterToggleable(questButton);
            _toggleGroup.RegisterToggleable(illustratedBookButton);
            _toggleGroup.RegisterToggleable(characterButton);
            _toggleGroup.RegisterToggleable(worldMapButton);
            _toggleGroup.RegisterToggleable(settingsButton);
            _toggleGroup.RegisterToggleable(chatButton);
            _toggleGroup.RegisterToggleable(combinationButton);

            SubmitWidget = null;
            CloseWidget = null;

            _buttonsPositionY = _buttons.position.y;
        }

        public override void Initialize()
        {
            base.Initialize();

            SharedModel.NavigationType.
                Subscribe(SubscribeNavigationType)
                .AddTo(gameObject);
            HasNotificationInMail
                .SubscribeTo(mailButton.SharedModel.HasNotification)
                .AddTo(gameObject);
            HasNotificationInQuest.
                SubscribeTo(questButton.SharedModel.HasNotification)
                .AddTo(gameObject);
            HasNotificationInChat
                .SubscribeTo(chatButton.SharedModel.HasNotification)
                .AddTo(gameObject);
            HasNotificationInIllustratedBook
                .SubscribeTo(illustratedBookButton.SharedModel.HasNotification)
                .AddTo(gameObject);
            HasNotificationInCharacter
                .SubscribeTo(characterButton.SharedModel.HasNotification)
                .AddTo(gameObject);
            HasNotificationInWorldMap
                .SubscribeTo(worldMapButton.SharedModel.HasNotification)
                .AddTo(gameObject);
            HasNotificationInSettings
                .SubscribeTo(settingsButton.SharedModel.HasNotification)
                .AddTo(gameObject);
            HasNotificationInCombination
                .SubscribeTo(combinationButton.SharedModel.HasNotification)
                .AddTo(gameObject);

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
            ReactiveAvatarState.MailBox?
                .Subscribe(SubscribeAvatarMailBox)
                .AddTo(_disposablesAtOnEnable);
            ReactiveAvatarState.QuestList?
                .Subscribe(SubscribeAvatarQuestList)
                .AddTo(_disposablesAtOnEnable);
        }

        protected override void OnDisable()
        {
            _toggleGroup.SetToggledOffAll();
            _disposablesAtOnEnable.DisposeAllAndClear();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SharedModel.Dispose();
            HasNotificationInMail.Dispose();
            HasNotificationInQuest.Dispose();
            HasNotificationInChat.Dispose();
            HasNotificationInIllustratedBook.Dispose();
            HasNotificationInCharacter.Dispose();
            HasNotificationInWorldMap.Dispose();
            HasNotificationInSettings.Dispose();
            HasNotificationInCombination.Dispose();
        }

        public void Show(
            UINavigator.NavigationType navigationType,
            Action<BottomMenu> navigationAction,
            bool animateAlpha = true,
            params ToggleableType[] showButtons)
        {
            CloseWidget = () => navigationAction?.Invoke(this);

            base.Show(animateAlpha);

            if (animateAlpha)
            {
                // FIXME: Widget의 연출 주기 캡슐화가 깨지는 부분이에요.
                AnimationState = AnimationStateType.Showing;
                var pos = _buttons.position;
                pos.y = _buttonsPositionY;
                _buttons.position = pos;

                canvasGroup.alpha = 0f;
                canvasGroup
                    .DOFade(1f, 1f)
                    .OnComplete(() => AnimationState = AnimationStateType.Shown);
            }
            else
            {
                canvasGroup.alpha = 1f;
            }

            SharedModel.NavigationType.SetValueAndForceNotify(navigationType);
            SharedModel.NavigationAction = navigationAction;

            foreach (var toggleableType in ToggleableTypes)
            {
                if (showButtons.Contains(toggleableType) && ShowButton(toggleableType))
                {
                    continue;
                }

                var button = GetButton(toggleableType);
                button.Hide();
            }
        }

        // 이 위젯은 애니메이션 없이 바로 닫히는 것을 기본으로 함.
        public override void Close(bool ignoreCloseAnimation = false)
        {
            canvasGroup.DOKill();

            foreach (var toggleable in _toggleGroup.Toggleables)
            {
                if (!(toggleable is IWidgetControllable widgetControllable))
                {
                    continue;
                }

                widgetControllable.HideWidget();
            }

            base.Close(ignoreCloseAnimation);
        }

        public void SetIntractable(bool intractable)
        {
            canvasGroup.interactable = intractable;
        }

        public void PlayGetItemAnimation()
        {
            characterButton.Animator.Play("GetItem");
            inventoryVFX.Play();
        }

        #region Subscribe

        private void SubscribeNavigationType(UINavigator.NavigationType navigationType)
        {
            switch (navigationType)
            {
                case UINavigator.NavigationType.None:
                    exitButton.Hide();
                    backButton.Hide();
                    mainButton.Hide();
                    quitButton.Hide();
                    break;
                case UINavigator.NavigationType.Back:
                    exitButton.Hide();
                    backButton.Show();
                    mainButton.Hide();
                    quitButton.Hide();
                    break;
                case UINavigator.NavigationType.Main:
                    exitButton.Hide();
                    backButton.Hide();
                    mainButton.Show();
                    quitButton.Hide();
                    break;
                case UINavigator.NavigationType.Exit:
                    exitButton.Show();
                    backButton.Hide();
                    mainButton.Hide();
                    quitButton.Hide();
                    break;
                case UINavigator.NavigationType.Quit:
                    exitButton.Hide();
                    backButton.Hide();
                    mainButton.Hide();
                    quitButton.Show();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(navigationType), navigationType,
                        null);
            }
        }

        private void SubscribeNavigationButtonClick()
        {
            SharedModel.NavigationAction?.Invoke(this);
        }

        private void SubscribeAvatarMailBox(MailBox mailBox)
        {
            if (mailBox is null)
            {
                Debug.LogWarning($"{nameof(mailBox)} is null.");
                return;
            }

            HasNotificationInMail.OnNext(mailBox.Any(i =>
                i.New && i.requiredBlockIndex <= _blockIndex));
        }

        private void SubscribeAvatarQuestList(QuestList questList)
        {
            if (questList is null)
            {
                Debug.LogWarning($"{nameof(questList)} is null.");
                return;
            }

            HasNotificationInQuest.OnNext(questList.Any(quest =>
                quest.IsPaidInAction && quest.isReceivable));
            // todo: `Quest`와의 결합을 끊을 필요가 있어 보임.
            Find<Quest>().SetList(questList);
        }

        private void SubscribeBlockIndex(long blockIndex)
        {
            _blockIndex = blockIndex;
            var mailBox = Find<Mail>().MailBox;
            if (!(mailBox is null))
            {
                HasNotificationInMail.OnNext(mailBox.Any(i =>
                    i.New && i.requiredBlockIndex <= _blockIndex));
            }

            UpdateCombinationNotification();
        }
        
        #endregion

        #region show button

        private bool ShowButton(ToggleableType toggleableType)
        {
            switch (toggleableType)
            {
                case ToggleableType.Mail:
                    return ShowMailButton();
                case ToggleableType.Quest:
                    return ShowQuestButton();
                case ToggleableType.Chat:
                    return ShowChatButton();
                case ToggleableType.IllustratedBook:
                    return ShowIllustratedBookButton();
                case ToggleableType.Character:
                    return ShowCharacterButton();
                case ToggleableType.WorldMap:
                    return ShowWorldMapButton();
                case ToggleableType.Settings:
                    return ShowSettingsButton();
                case ToggleableType.Combination:
                    return ShowCombinationButton();
                default:
                    throw new ArgumentOutOfRangeException(nameof(toggleableType), toggleableType,
                        null);
            }
        }

        private bool ShowChatButton()
        {
            if (!States.Instance.CurrentAvatarState.worldInformation
                .TryGetUnlockedWorldByStageClearedBlockIndex(
                    out var world))
            {
                return false;
            }

            if (world.StageClearedId < GameConfig.RequireClearedStageLevel.UIBottomMenuChat)
            {
                return false;
            }

            chatButton.Show();
            return true;
        }

        private bool ShowMailButton()
        {
            if (!States.Instance.CurrentAvatarState.worldInformation
                .TryGetUnlockedWorldByStageClearedBlockIndex(
                    out var world))
            {
                return false;
            }

            if (world.StageClearedId < GameConfig.RequireClearedStageLevel.UIBottomMenuMail)
            {
                return false;
            }

            // todo: 제조 시도 후인지 추가 검사.

            mailButton.Show();
            return true;
        }

        private bool ShowQuestButton()
        {
            if (!States.Instance.CurrentAvatarState.worldInformation
                .TryGetUnlockedWorldByStageClearedBlockIndex(
                    out var world))
            {
                return false;
            }

            if (world.StageClearedId < GameConfig.RequireClearedStageLevel.UIBottomMenuQuest)
            {
                return false;
            }

            questButton.Show();
            return true;
        }

        private bool ShowIllustratedBookButton()
        {
            return false;
        }

        private bool ShowCharacterButton()
        {
            if (!States.Instance.CurrentAvatarState.worldInformation
                .TryGetUnlockedWorldByStageClearedBlockIndex(
                    out var world))
            {
                return false;
            }

            if (world.StageClearedId < GameConfig.RequireClearedStageLevel.UIBottomMenuCharacter)
            {
                return false;
            }

            characterButton.Show();
            return true;
        }

        private bool ShowWorldMapButton()
        {
            worldMapButton.Show();
            return true;
        }

        private bool ShowSettingsButton()
        {
            if (!States.Instance.CurrentAvatarState.worldInformation
                .TryGetUnlockedWorldByStageClearedBlockIndex(
                    out var world))
            {
                return false;
            }

            if (world.StageClearedId < GameConfig.RequireClearedStageLevel.UIBottomMenuSettings)
            {
                return false;
            }

            settingsButton.Show();
            return true;
        }

        private bool ShowCombinationButton()
        {
            if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(GameConfig
                .RequireClearedStageLevel.CombinationEquipmentAction))
            {
                return false;
            }

            combinationButton.Show();
            return true;
        }

        #endregion

        private ToggleableButton GetButton(ToggleableType toggleableType)
        {
            switch (toggleableType)
            {
                case ToggleableType.Mail:
                    return mailButton;
                case ToggleableType.Quest:
                    return questButton;
                case ToggleableType.Chat:
                    return chatButton;
                case ToggleableType.IllustratedBook:
                    return illustratedBookButton;
                case ToggleableType.Character:
                    return characterButton;
                case ToggleableType.WorldMap:
                    return worldMapButton;
                case ToggleableType.Settings:
                    return settingsButton;
                case ToggleableType.Combination:
                    return combinationButton;
                default:
                    throw new ArgumentOutOfRangeException(nameof(toggleableType), toggleableType,
                        null);
            }
        }

        public void UpdateCombinationNotification()
        {
            var combinationSlots = Find<CombinationSlots>().slots;
            var hasNotification = combinationSlots.Any(slot => slot.HasNotification.Value);
            HasNotificationInCombination.OnNext(hasNotification);
        }
    }
}
