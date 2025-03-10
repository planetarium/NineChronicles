using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.ApiClient;
using Nekoyume.Blockchain;
using Nekoyume.Game.Controller;
using Nekoyume.Game.LiveAsset;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Multiplanetary;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    using UniRx;

    public class EventRewardPopup : PopupWidget
    {
        [Serializable]
        private struct EventImage
        {
            public GameObject container;
            public Image image;
        }

        [Serializable]
        private struct EventToggle
        {
            public Toggle toggle;
            public TextMeshProUGUI disabledText;
            public TextMeshProUGUI enabledText;

            public void SetText(string text)
            {
                disabledText.text = text;
                enabledText.text = text;
            }
        }

        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private EventToggle[] tabToggles;
        [SerializeField] private TextMeshProUGUI eventPeriodText;

        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private EventImage eventImage;
        [SerializeField] private PatrolRewardModule patrolRewardModule;
        [SerializeField] private ConditionalButton[] actionButtons;
        [SerializeField] private ConditionalButton receiveButton;
        [SerializeField] private GameObject receiveButtonIndicator;

        private bool _isPatrolRewardInitialized;
        private bool _isInitialized;
        private readonly List<IDisposable> _disposables = new ();

        private const string LastReadingDayKey = "EVENT_REWARD_POPUP_LAST_READING_DAY";
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

        public bool HasUnread
        {
            get
            {
                var notReadAtToday = true;
                if (PlayerPrefs.HasKey(LastReadingDayKey) &&
                    DateTime.TryParseExact(PlayerPrefs.GetString(LastReadingDayKey),
                        DateTimeFormat, null, DateTimeStyles.None, out var result))
                {
                    notReadAtToday = DateTime.Today != result.Date;
                }

                return notReadAtToday;
            }
        }

        public bool HasEvent
        {
            get
            {
                var liveAssetManager = LiveAssetManager.instance;
                if (!liveAssetManager.IsInitialized || !_isInitialized)
                {
                    return false;
                }

                var eventRewardPopupData = liveAssetManager.EventRewardPopupData;
                return eventRewardPopupData.HasEvent;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
            });
            CloseWidget = () => Close(true);
        }

        public override void Initialize()
        {
            var liveAssetManager = LiveAssetManager.instance;
            if (!liveAssetManager.IsInitialized || _isInitialized)
            {
                return;
            }

            var eventRewardPopupData = liveAssetManager.EventRewardPopupData;
            titleText.text = L10nManager.Localize(eventRewardPopupData.TitleL10NKey);

            for (var i = 0; i < tabToggles.Length; i++)
            {
                var tabToggle = tabToggles[i];
                if (i >= eventRewardPopupData.EventRewards.Length)
                {
                    tabToggle.toggle.gameObject.SetActive(false);
                    continue;
                }

                var eventReward = eventRewardPopupData.EventRewards[i];
                System.Action setContent = eventReward.ContentPresetType switch
                {
                    EventRewardPopupData.ContentPresetType.None => () => SetContent(eventReward.Content),
                    EventRewardPopupData.ContentPresetType.ClaimGift => () => SetClaimGift(eventReward.Content),
                    EventRewardPopupData.ContentPresetType.PatrolReward => SetPatrolReward,
                    EventRewardPopupData.ContentPresetType.ThorChain => () => SetThorChain(eventRewardPopupData),
                    _ => null,
                };
                tabToggle.toggle.onValueChanged.AddListener(value =>
                {
                    if (!value)
                    {
                        return;
                    }

                    SetData(eventReward);
                    setContent?.Invoke();
                });
                tabToggle.SetText(L10nManager.Localize(eventReward.ToggleL10NKey));
                tabToggle.toggle.gameObject.SetActive(true);
            }

            _isInitialized = true;
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            var eventRewardData = LiveAssetManager.instance.EventRewardPopupData;
            var eventRewards = eventRewardData.EventRewards;
            if (!eventRewardData.HasEvent)
            {
                NcDebug.LogError("No event rewards.");
                return;
            }

            if (!_isPatrolRewardInitialized)
            {
                patrolRewardModule.Initialize();
                _isPatrolRewardInitialized = true;
            }

            int index;
            if (TryGetClaimableGift(out _))
            {
                var claimGifts = eventRewards.FirstOrDefault(reward =>
                    reward.ContentPresetType == EventRewardPopupData.ContentPresetType.ClaimGift);
                index = Array.IndexOf(eventRewards, claimGifts);
                if (index >= 0)
                {
                    ShowAsTab(index);
                    return;
                }
            }

            var other = eventRewards.FirstOrDefault(reward =>
                reward.ContentPresetType != EventRewardPopupData.ContentPresetType.ClaimGift);
            index = Array.IndexOf(eventRewards, other);

            ShowAsTab(Mathf.Max(index, 0));
        }

        public void ShowAsThorChain()
        {
            var eventRewardData = LiveAssetManager.instance.EventRewardPopupData;
            var eventRewards = eventRewardData.EventRewards;
            if (!eventRewardData.HasEvent)
            {
                NcDebug.LogError("No event rewards.");
                return;
            }

            var thor = eventRewards.FirstOrDefault(reward =>
                reward.ContentPresetType == EventRewardPopupData.ContentPresetType.ThorChain);
            var index = Array.IndexOf(eventRewards, thor);

            ShowAsTab(Mathf.Max(index, 0));
        }

        public void ShowAsPatrolReward()
        {
            var eventRewardData = LiveAssetManager.instance.EventRewardPopupData;
            var eventRewards = eventRewardData.EventRewards;
            if (!eventRewardData.HasEvent)
            {
                NcDebug.LogError("No event rewards.");
                return;
            }

            var patrolReward = eventRewards.FirstOrDefault(reward =>
                reward.ContentPresetType == EventRewardPopupData.ContentPresetType.PatrolReward);
            var index = Array.IndexOf(eventRewards, patrolReward);

            ShowAsTab(Mathf.Max(index, 0));
        }

        private void ShowAsTab(int index, bool ignoreShowAnimation = false)
        {
            if (index < 0 || index >= tabToggles.Length)
            {
                NcDebug.LogError($"Invalid index: {index}");
                return;
            }

            base.Show(ignoreShowAnimation);

            if (!_isInitialized)
            {
                Initialize();
            }

            // init toggle state
            var thorToggle = tabToggles[index].toggle;
            thorToggle.isOn = false;
            thorToggle.isOn = true;

            PlayerPrefs.SetString(LastReadingDayKey, DateTime.Today.ToString(DateTimeFormat));
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);

            _disposables.DisposeAllAndClear();
        }

        private void SetData(EventRewardPopupData.EventReward eventReward)
        {
            _disposables.DisposeAllAndClear();

            var begin = DateTime
                .ParseExact(eventReward.BeginDateTime, "yyyy-MM-ddTHH:mm:ss", null)
                .ToString("M/d", CultureInfo.InvariantCulture);
            var end = DateTime
                .ParseExact(eventReward.EndDateTime, "yyyy-MM-ddTHH:mm:ss", null)
                .ToString("M/d", CultureInfo.InvariantCulture);
            eventPeriodText.text = $"{L10nManager.Localize("UI_EVENT_PERIOD")} : {begin} - {end}";
            descriptionText.text = L10nManager.Localize(eventReward.DescriptionL10NKey);

            eventImage.container.SetActive(false);
            patrolRewardModule.gameObject.SetActive(false);

            receiveButton.gameObject.SetActive(false);
            foreach (var actionButton in actionButtons)
            {
                actionButton.gameObject.SetActive(false);
            }
        }

        private void SetClaimGift(EventRewardPopupData.Content content)
        {
            SetImage(content.Image);

            receiveButton.gameObject.SetActive(true);
            receiveButton.OnSubmitSubject
                .Subscribe(_ => ClaimGifts())
                .AddTo(_disposables);

            LoadingHelper.ClaimGifts.Subscribe(value =>
            {
                receiveButton.Text = value
                    ? string.Empty
                    : L10nManager.Localize("UI_GET_REWARD");
                receiveButtonIndicator.SetActive(value);
                receiveButton.Interactable = TryGetClaimableGift(out _) && !value;
            }).AddTo(_disposables);
        }

        private void SetPatrolReward()
        {
            patrolRewardModule.SetData();
            patrolRewardModule.gameObject.SetActive(true);
            receiveButton.gameObject.SetActive(true);

            PatrolReward.PatrolTime
                .Where(_ => !PatrolReward.Claiming.Value)
                .Select(patrolTime => PatrolReward.Interval - patrolTime)
                .Subscribe(SetReceiveButton)
                .AddTo(_disposables);

            PatrolReward.Claiming.Where(claiming => claiming)
                .Subscribe(_ =>
                {
                    receiveButton.Interactable = false;
                    receiveButton.Text = string.Empty;
                    receiveButtonIndicator.SetActive(true);
                })
                .AddTo(_disposables);

            receiveButton.OnSubmitSubject
                .Subscribe(_ => ClaimPatrolReward())
                .AddTo(_disposables);
        }

        private void SetThorChain(EventRewardPopupData popupData)
        {
            var thorSchedule = LiveAssetManager.instance.ThorSchedule;
            var isOpened = thorSchedule != null && thorSchedule.IsOpened;
            var content = isOpened
                ? popupData.EnabledThorChainContent
                : popupData.DisabledThorChainContent;
            SetImage(content.Image);

            if (!isOpened)
            {
                return;
            }

            var currentPlanetId = Game.Game.instance.CurrentPlanetId;
            var isPlanetThor = currentPlanetId.HasValue && PlanetId.IsThor(currentPlanetId.Value);
            if (isPlanetThor)
            {
                SetShortcutButtons(content.ShortcutTypes);
            }
            else
            {
                var button = actionButtons.First();
                button.gameObject.SetActive(true);
                button.Interactable = true;
                button.Text = L10nManager.Localize("UI_PARTICIPATE");
                button.OnSubmitSubject
                    .Subscribe(_ => ShowQuitConfirmPopup())
                    .AddTo(_disposables);
            }
        }

        private void SetContent(EventRewardPopupData.Content content)
        {
            SetImage(content.Image);
            SetShortcutButtons(content.ShortcutTypes);
        }

        private void SetImage(Sprite image)
        {
            eventImage.container.SetActive(true);
            eventImage.image.sprite = image;
        }

        private void SetShortcutButtons(ShortcutHelper.PlaceType[] shortcutTypes)
        {
            for (var i = 0; i < shortcutTypes.Length; i++)
            {
                var shortcutType = shortcutTypes[i];

                var button = actionButtons[i];
                button.gameObject.SetActive(true);
                button.Interactable = ShortcutHelper.CheckConditionOfShortcut(shortcutType);

                var shortcut = ShortcutHelper.GetAcquisitionPlace(this, shortcutType);
                button.Text = shortcut.GuideText;
                button.OnSubmitSubject
                    .Subscribe(_ => shortcut.OnClick?.Invoke())
                    .AddTo(_disposables);
            }
        }

        private void ClaimGifts()
        {
            if (!TryGetClaimableGift(out var row))
            {
                NcDebug.LogError("No claimable gift.");
                return;
            }

            LoadingHelper.ClaimGifts.Value = true;

            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            ActionManager.Instance.ClaimGifts(avatarAddress, row.Id);
        }

        private void ClaimPatrolReward()
        {
            Analyzer.Instance.Track("Unity/PatrolReward/Request Claim Reward");

            PatrolReward.ClaimReward(null);
        }

        // subscribe from PatrolReward.PatrolTime
        public void SetReceiveButton(long remainTime)
        {
            var canReceive = remainTime <= 0L;
            receiveButton.Interactable = canReceive;
            receiveButton.Text = canReceive
                ? L10nManager.Localize("UI_GET_REWARD")
                : L10nManager.Localize("UI_REMAINING_TIME",
                    remainTime.BlockRangeToTimeSpanString());
            receiveButtonIndicator.SetActive(false);
        }

        private static bool TryGetClaimableGift(out ClaimableGiftsSheet.Row row)
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var sheet = Game.Game.instance.TableSheets.ClaimableGiftsSheet;
            var claimedGiftIds = Game.Game.instance.States.ClaimedGiftIds;
            if (claimedGiftIds != null)
            {
                row = sheet.OrderedList.FirstOrDefault(r =>
                    r.Validate(blockIndex) && !claimedGiftIds.Contains(r.Id));
                return row != null;
            }

            row = null;
            return false;
        }

        private static void ShowQuitConfirmPopup()
        {
            var confirm = Find<IconAndButtonSystem>();
            confirm.ShowWithTwoButton(
                "UI_CONFIRM",
                "UI_QUIT_FOR_THOR_CHAIN_CONFIRM",
                type: IconAndButtonSystem.SystemType.Information);
            confirm.SetConfirmCallbackToExit();
            confirm.CancelCallback = () => confirm.Close();
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickClaimPatrolRewardButtonInEvent()
        {
            receiveButton.OnSubmitSubject.OnNext(default);
            Close();
        }
    }
}
