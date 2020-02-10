using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Quest;
using Nekoyume.State;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class BottomMenu : Widget
    {
        public enum ToggleableType
        {
            Mail,
            Quest,
            Chat,
            IllustratedBook,
            Character,
            Inventory,
            WorldMap,
            Settings
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
        public NotifiableButton inventoryButton;
        public NotifiableButton worldMapButton;
        public NotifiableButton settingsButton;
        
        public CanvasGroup canvasGroup;
        private Animator _inventoryAnimator;

        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();

        public readonly Model SharedModel = new Model();
        public readonly Subject<bool> HasNotificationInMail = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInQuest = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInChat = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInIllustratedBook = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInCharacter = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInInventory = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInWorldMap = new Subject<bool>();
        public readonly Subject<bool> HasNotificationInSettings = new Subject<bool>();

        public override WidgetType WidgetType => WidgetType.Popup;

        protected override void Awake()
        {
            base.Awake();

            backButton.button.OnClickAsObservable().Subscribe(SubscribeNavigationButtonClick).AddTo(gameObject);
            mainButton.button.OnClickAsObservable().Subscribe(SubscribeNavigationButtonClick).AddTo(gameObject);
            quitButton.button.OnClickAsObservable().Subscribe(SubscribeNavigationButtonClick).AddTo(gameObject);
            exitButton.button.OnClickAsObservable().Subscribe(SubscribeNavigationButtonClick).AddTo(gameObject);

            quitButton.SetWidgetType<Confirm>();
            exitButton.SetWidgetType<Confirm>();
            mailButton.SetWidgetType<Mail>();
            questButton.SetWidgetType<Quest>();
            characterButton.SetWidgetType<StatusDetail>();
            inventoryButton.SetWidgetType<UI.Inventory>();
            settingsButton.SetWidgetType<Settings>();
            chatButton.SetWidgetType<Confirm>();
            // todo: 지금 월드맵 띄우는 것을 위젯으로 빼고, 여기서 설정하기?
            // worldMapButton.SetWidgetType<WorldMapPaper>();

            chatButton.button.OnClickAsObservable().Subscribe(SubScribeOnClickChat).AddTo(gameObject);
            // 미구현
            illustratedBookButton.button.OnClickAsObservable().Subscribe(SubscribeOnClick).AddTo(gameObject);
            illustratedBookButton.SetWidgetType<Alert>();

            _toggleGroup = new ToggleGroup();
            _toggleGroup.RegisterToggleable(quitButton);
            _toggleGroup.RegisterToggleable(exitButton);
            _toggleGroup.RegisterToggleable(mailButton);
            _toggleGroup.RegisterToggleable(questButton);
            _toggleGroup.RegisterToggleable(illustratedBookButton);
            _toggleGroup.RegisterToggleable(characterButton);
            _toggleGroup.RegisterToggleable(inventoryButton);
            _toggleGroup.RegisterToggleable(worldMapButton);
            _toggleGroup.RegisterToggleable(settingsButton);
            _toggleGroup.RegisterToggleable(chatButton);
            
            SubmitWidget = null;
            CloseWidget = null;
            
            _inventoryAnimator = inventoryButton.GetComponent<Animator>();
        }

        public override void Initialize()
        {
            base.Initialize();

            SharedModel.NavigationType.Subscribe(SubscribeNavigationType).AddTo(gameObject);
            HasNotificationInMail.SubscribeTo(mailButton.SharedModel.HasNotification).AddTo(gameObject);
            HasNotificationInQuest.SubscribeTo(questButton.SharedModel.HasNotification).AddTo(gameObject);
            HasNotificationInChat.SubscribeTo(chatButton.SharedModel.HasNotification).AddTo(gameObject);
            HasNotificationInIllustratedBook.SubscribeTo(illustratedBookButton.SharedModel.HasNotification)
                .AddTo(gameObject);
            HasNotificationInCharacter.SubscribeTo(characterButton.SharedModel.HasNotification)
                .AddTo(gameObject);
            HasNotificationInInventory.SubscribeTo(inventoryButton.SharedModel.HasNotification)
                .AddTo(gameObject);
            HasNotificationInWorldMap.SubscribeTo(worldMapButton.SharedModel.HasNotification)
                .AddTo(gameObject);
            HasNotificationInSettings.SubscribeTo(settingsButton.SharedModel.HasNotification)
                .AddTo(gameObject);
        } 

        private void SubScribeOnClickChat(Unit unit)
        {
            var confirm = Find<Confirm>();
            confirm.CloseCallback = result =>
            {
                if (result == ConfirmResult.No)
                    return;
                Application.OpenURL(GameConfig.DiscordLink);
            };
            confirm.Set("UI_PROCEED_DISCORD", "UI_PROCEED_DISCORD_CONTENT", blurRadius: 2);
        }

        private void SubscribeOnClick(Unit unit)
        {
            Find<Alert>().Set("UI_ALERT_NOT_IMPLEMENTED_TITLE", "UI_ALERT_NOT_IMPLEMENTED_CONTENT", blurRadius: 2);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _disposablesAtOnEnable.DisposeAllAndClear();
            ReactiveAvatarState.MailBox?.Subscribe(SubscribeAvatarMailBox).AddTo(_disposablesAtOnEnable);
            ReactiveAvatarState.QuestList?.Subscribe(SubscribeAvatarQuestList).AddTo(_disposablesAtOnEnable);
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
            HasNotificationInInventory.Dispose();
            HasNotificationInWorldMap.Dispose();
            HasNotificationInSettings.Dispose();
        }

        public void Show(UINavigator.NavigationType navigationType, Action<BottomMenu> navigationAction,
            bool animateAlpha = true, params ToggleableType[] showButtons)
        {
            CloseWidget = () => navigationAction?.Invoke(this);
         
            base.Show();
            if(animateAlpha)
            {
                Animator.enabled = false;
                
                canvasGroup.alpha = 0;
                canvasGroup.DOFade(1,  1.0f);
            }
            
            SharedModel.NavigationType.SetValueAndForceNotify(navigationType);
            SharedModel.NavigationAction = navigationAction;

            mailButton.Hide();
            questButton.Hide();
            chatButton.Hide();
            illustratedBookButton.Hide();
            characterButton.Hide();
            inventoryButton.Hide();
            worldMapButton.Hide();
            settingsButton.Hide();

            foreach (var toggleableType in showButtons)
            {
                switch (toggleableType)
                {
                    case ToggleableType.Mail:
                        mailButton.Show();
                        break;
                    case ToggleableType.Quest:
                        questButton.Show();
                        break;
                    case ToggleableType.Chat:
                        chatButton.Show();
                        break;
                    case ToggleableType.IllustratedBook:
                        illustratedBookButton.Show();
                        break;
                    case ToggleableType.Character:
                        characterButton.Show();
                        break;
                    case ToggleableType.Inventory:
                        inventoryButton.Show();
                        break;
                    case ToggleableType.WorldMap:
                        worldMapButton.Show();
                        break;
                    case ToggleableType.Settings:
                        settingsButton.Show();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        // 이 위젯은 애니메이션 없이 바로 닫히는 것을 기본으로 함.
        public override void Close(bool ignoreCloseAnimation = false)
        {
            foreach (var toggleable in _toggleGroup.Toggleables)
            {
                if (!(toggleable is IWidgetControllable widgetControllable))
                    continue;

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
            if(_inventoryAnimator)
                _inventoryAnimator.Play("GetItem");
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
                    throw new ArgumentOutOfRangeException(nameof(navigationType), navigationType, null);
            }
        }

        private void SubscribeNavigationButtonClick(Unit unit)
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
            
            HasNotificationInMail.OnNext(mailBox.Any(i => i.New));
            // todo: `Mail`과의 결합을 끊을 필요가 있어 보임.
            Find<Mail>().SetList(mailBox);
        }

        private void SubscribeAvatarQuestList(QuestList questList)
        {
            if (questList is null)
            {
                Debug.LogWarning($"{nameof(questList)} is null.");
                return;
            }

            HasNotificationInQuest.OnNext(questList.Any(quest => quest.IsPaidInAction && quest.isReceivable));
            // todo: `Quest`와의 결합을 끊을 필요가 있어 보임.
            Find<Quest>().SetList(questList);
        }

        #endregion

        #region show button

        private void ShowChatButton()
        {
            if (!States.Instance.CurrentAvatarState.worldInformation.TryGetUnlockedWorldByLastStageClearedAt(
                out var world))
                return;

            if (world.StageClearedId < GameConfig.RequireStage.UIBottomMenuChat)
                return;
            
            chatButton.Show();
        }

        private void ShowMailButton()
        {
            if (!States.Instance.CurrentAvatarState.worldInformation.TryGetUnlockedWorldByLastStageClearedAt(
                out var world))
                return;

            if (world.StageClearedId < GameConfig.RequireStage.UIBottomMenuMail)
                return;
            
            // todo: 제조 시도 후인지 추가 검사.
            
            mailButton.Show();
        }

        private void ShowQuestButton()
        {
            if (!States.Instance.CurrentAvatarState.worldInformation.TryGetUnlockedWorldByLastStageClearedAt(
                out var world))
                return;

            if (world.StageClearedId < GameConfig.RequireStage.UIBottomMenuQuest)
                return;
            
            questButton.Show();
        }

        private void ShowIllustratedBookButton()
        {
            return;
        }

        private void ShowCharacterButton()
        {
            if (!States.Instance.CurrentAvatarState.worldInformation.TryGetUnlockedWorldByLastStageClearedAt(
                out var world))
                return;

            if (world.StageClearedId < GameConfig.RequireStage.UIBottomMenuCharacter)
                return;
            
            characterButton.Show();
        }

        private void ShowInventoryButton()
        {
            if (!States.Instance.CurrentAvatarState.worldInformation.TryGetUnlockedWorldByLastStageClearedAt(
                out var world))
                return;

            if (world.StageClearedId < GameConfig.RequireStage.UIBottomMenuInventory)
                return;
            
            inventoryButton.Show();
        }

        private void ShowWorldMapButton()
        {
            worldMapButton.Show();
        }

        private void ShowSettingsButton()
        {
            if (!States.Instance.CurrentAvatarState.worldInformation.TryGetUnlockedWorldByLastStageClearedAt(
                out var world))
                return;

            if (world.StageClearedId < GameConfig.RequireStage.UIBottomMenuSettings)
                return;
            
            settingsButton.Show();
        }
        
        #endregion
        
    }
}
