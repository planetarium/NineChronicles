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

        private bool _isInitialized;
        private readonly List<IDisposable> _disposables = new ();

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

            var eventRewards = eventRewardPopupData.EventRewards
                .Where(reward => DateTime.UtcNow.IsInTime(reward.BeginDateTime, reward.EndDateTime))
                .ToList();
            for (var i = 0; i < tabToggles.Length; i++)
            {
                var tabToggle = tabToggles[i];
                if (i >= eventRewards.Count)
                {
                    tabToggle.toggle.gameObject.SetActive(false);
                    continue;
                }

                var eventReward = eventRewards[i];
                System.Action action = eventReward.ContentPresetType switch
                {
                    EventRewardPopupData.ContentPresetType.None => () => SetContent(eventReward.Content),
                    EventRewardPopupData.ContentPresetType.ClaimGift => () => SetClaimGift(eventReward.Content),
                    EventRewardPopupData.ContentPresetType.PatrolReward => SetPatrolReward,
                    EventRewardPopupData.ContentPresetType.ThorChain => SetThorChain,
                    _ => null,
                };
                tabToggle.toggle.onValueChanged.AddListener(value =>
                {
                    if (value)
                    {
                        SetData(eventReward);
                    }
                });
                tabToggle.SetText(L10nManager.Localize(eventReward.ToggleL10NKey));
                tabToggle.toggle.gameObject.SetActive(true);
            }

            _isInitialized = true;
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            if (_isInitialized)
            {
                Initialize();
            }

            // init toggle state
            var defaultToggle = tabToggles.First().toggle;
            defaultToggle.isOn = false;
            defaultToggle.isOn = true;
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
            eventImage.container.SetActive(true);
            eventImage.image.sprite = content.Image;

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
                receiveButton.Interactable = TryGetClaimableGifts(out _) && !value;
            }).AddTo(_disposables);
        }

        private async void SetPatrolReward()
        {
            await patrolRewardModule.SetData();
            patrolRewardModule.gameObject.SetActive(true);
            receiveButton.gameObject.SetActive(true);

            PatrolReward.PatrolTime
                .Where(_ => !PatrolReward.Claiming.Value)
                .Select(patrolTime =>
                {
                    var patrolTimeWithOutSeconds = new TimeSpan(patrolTime.Ticks /
                        TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
                    return PatrolReward.Interval - patrolTimeWithOutSeconds;
                })
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

        private void SetThorChain()
        {
            var thorSchedule = LiveAssetManager.instance.ThorSchedule;
            var isOpened = thorSchedule != null && thorSchedule.IsOpened;
        }

        private void SetContent(EventRewardPopupData.Content content)
        {
            eventImage.container.SetActive(true);
            eventImage.image.sprite = content.Image;

            for (var i = 0; i < content.ShortcutTypes.Length; i++)
            {
                var shortcutType = content.ShortcutTypes[i];

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
            if (!TryGetClaimableGifts(out var row))
            {
                NcDebug.LogError("No claimable gifts.");
                return;
            }

            LoadingHelper.ClaimGifts.Value = true;

            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            ActionManager.Instance.ClaimGifts(avatarAddress, row.Id);

            Debug.LogError($"Claimed one-time gift. {row.Id}");
        }

        private void ClaimPatrolReward()
        {
            Analyzer.Instance.Track("Unity/PatrolReward/Request Claim Reward");

            var evt = new AirbridgeEvent("PatrolReward_Request_Claim_Reward");
            AirbridgeUnity.TrackEvent(evt);

            PatrolReward.ClaimReward(null);
        }

        // subscribe from PatrolReward.PatrolTime
        private void SetReceiveButton(TimeSpan remainTime)
        {
            var canReceive = remainTime <= TimeSpan.Zero;
            receiveButton.Interactable = canReceive;
            receiveButton.Text = canReceive
                ? L10nManager.Localize("UI_GET_REWARD")
                : L10nManager.Localize("UI_REMAINING_TIME",
                    PatrolRewardModule.TimeSpanToString(remainTime));
            receiveButtonIndicator.SetActive(false);
        }

        private static bool TryGetClaimableGifts(out ClaimableGiftsSheet.Row row)
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var sheet = Game.Game.instance.TableSheets.ClaimableGiftsSheet;
            var claimedGiftIds = Game.Game.instance.States.ClaimedGiftIds;
            if (claimedGiftIds != null)
            {
                return sheet.TryFindRowByBlockIndex(blockIndex, out row) && !claimedGiftIds.Contains(row.Id);
            }

            row = null;
            return false;
        }
    }
}
