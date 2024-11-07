using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.ApiClient;
using Nekoyume.Blockchain;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
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

        [SerializeField] private Button closeButton;
        [SerializeField] private Toggle[] tabToggles;
        [SerializeField] private TextMeshProUGUI eventPeriodText;

        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private EventImage eventImage;
        [SerializeField] private PatrolRewardModule patrolRewardModule;
        [SerializeField] private ConditionalButton[] actionButtons;
        [SerializeField] private ConditionalButton receiveButton;
        [SerializeField] private GameObject receiveButtonIndicator;

        [SerializeField] private Sprite[] eventBannerSprites;

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

            for (var i = 0; i < tabToggles.Length; i++)
            {
                var index = i;
                tabToggles[i].onValueChanged.AddListener(value =>
                {
                    if (value)
                    {
                        SetData(index);

                        switch (index)
                        {
                            case 0:
                                SetCustomGift();
                                break;
                            case 1:
                                SetPatrolReward();
                                break;
                            case 2:
                                SetShopPackage();
                                break;
                            case 3:
                                SetDoubleContentsRewards();
                                break;
                        }
                    }
                });
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);

            tabToggles.First().isOn = false;
            tabToggles.First().isOn = true;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);

            _disposables.DisposeAllAndClear();
        }

        private void SetData(int index)
        {
            _disposables.DisposeAllAndClear();

            descriptionText.text = L10nManager.Localize($"UI_EVENT_DESCRIPTION_{index}");

            eventImage.container.SetActive(false);
            eventImage.image.sprite = eventBannerSprites[index];
            patrolRewardModule.gameObject.SetActive(false);

            receiveButton.gameObject.SetActive(false);
            foreach (var actionButton in actionButtons)
            {
                actionButton.gameObject.SetActive(false);
            }
        }

        private void SetCustomGift()
        {
            eventImage.container.SetActive(true);

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

        private void SetShopPackage()
        {
            eventImage.container.SetActive(true);

            var button = actionButtons.First();
            button.gameObject.SetActive(true);
            button.Interactable =
                ShortcutHelper.CheckConditionOfShortcut(ShortcutHelper.PlaceType.MobileShop);

            var shortcut =
                ShortcutHelper.GetAcquisitionPlace(this, ShortcutHelper.PlaceType.MobileShop);
            button.Text = shortcut.GuideText;
            button.OnSubmitSubject.Subscribe(_ => shortcut.OnClick?.Invoke()).AddTo(_disposables);
        }

        private void SetDoubleContentsRewards()
        {
            eventImage.container.SetActive(true);

            var shortcutTypes = new[]
            {
                ShortcutHelper.PlaceType.Arena,
                ShortcutHelper.PlaceType.AdventureBoss,
                ShortcutHelper.PlaceType.WorldBoss,
            };

            for (var i = 0; i < shortcutTypes.Length; i++)
            {
                var button = actionButtons[i];
                button.gameObject.SetActive(true);
                button.Interactable = ShortcutHelper.CheckConditionOfShortcut(shortcutTypes[i]);

                var shortcut = ShortcutHelper.GetAcquisitionPlace(this, shortcutTypes[i]);
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
