using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.ApiClient;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;
using ToggleGroup = Nekoyume.UI.Module.ToggleGroup;

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

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

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
                var toggle = tabToggles[i];
                var index = i;
                toggle.onClickToggle.AddListener(() => SetData(index));
                toggle.onClickObsoletedToggle.AddListener(() =>
                {
                    OneLineSystem.Push(
                        MailType.System,
                        L10nManager.Localize("NOTIFICATION_COMING_SOON"),
                        NotificationCell.NotificationType.Information);
                });
            }

        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
        }

        private void SetData(int index)
        {
            _disposables.DisposeAllAndClear();

            descriptionText.text = L10nManager.Localize($"UI_EVENT_DESCRIPTION_{index}");

            eventImage.container.SetActive(false);
            patrolRewardModule.gameObject.SetActive(false);

            receiveButton.gameObject.SetActive(false);
            foreach (var actionButton in actionButtons)
            {
                actionButton.gameObject.SetActive(false);
            }

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

        private void SetCustomGift()
        {
            eventImage.container.SetActive(true);

            receiveButton.gameObject.SetActive(true);
            receiveButton.Interactable = true;
            receiveButton.Text = L10nManager.Localize("UI_GET_REWARD");

            receiveButton.OnSubmitSubject
                .Subscribe(_ => ClaimExtraReward())
                .AddTo(_disposables);
        }

        private async void SetPatrolReward()
        {
            await patrolRewardModule.SetData();
            patrolRewardModule.gameObject.SetActive(true);
            receiveButton.gameObject.SetActive(true);

            PatrolReward.PatrolTime
                .Select(patrolTime =>
                {
                    var patrolTimeWithOutSeconds = new TimeSpan(patrolTime.Ticks /
                        TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
                    return PatrolReward.Interval - patrolTimeWithOutSeconds;
                })
                .Subscribe(remainTime => SetReceiveButton(remainTime, PatrolReward.Claiming.Value))
                .AddTo(_disposables);

            PatrolReward.Claiming.Where(claiming => claiming)
                .Subscribe(_ => receiveButton.Interactable = false)
                .AddTo(_disposables);

            receiveButton.OnSubmitSubject
                .Subscribe(_ => ClaimPatrolReward())
                .AddTo(_disposables);
        }

        private void SetShopPackage()
        {
            var button = actionButtons.First();

            button.gameObject.SetActive(true);
            button.Interactable = true; // Todo : Only for mobile
            button.Text = L10nManager.Localize("UI_SHOP_MOBILE");

            button.OnSubmitSubject.Subscribe(_ =>
            {
                Close();
                // TODO: Open Shop
            }).AddTo(_disposables);
        }

        private void SetDoubleContentsRewards()
        {
            for (int i = 0; i < 3; i++)
            {
                var button = actionButtons[i];
                button.gameObject.SetActive(true);
                button.Interactable = true;
            }

            actionButtons[0].Text = L10nManager.Localize("UI_MAIN_MENU_RANKING");
            actionButtons[0].OnSubmitSubject.Subscribe(_ =>
            {
                Close();
                // TODO: Open Ranking
            }).AddTo(_disposables);
            actionButtons[1].Text = L10nManager.Localize("UI_ADVENTURE_BODD_BACK_BUTTON");
            actionButtons[1].OnSubmitSubject.Subscribe(_ =>
            {
                Close();
                // TODO: Open Adventure Boss
            }).AddTo(_disposables);
            actionButtons[2].Text = L10nManager.Localize("UI_MAIN_MENU_WORLDBOSS");
            actionButtons[2].OnSubmitSubject.Subscribe(_ =>
            {
                Close();
                // TODO: Open World Boss
            }).AddTo(_disposables);
        }

        private void ClaimExtraReward()
        {

        }

        private void ClaimPatrolReward()
        {
            Analyzer.Instance.Track("Unity/PatrolReward/Request Claim Reward");

            var evt = new AirbridgeEvent("PatrolReward_Request_Claim_Reward");
            AirbridgeUnity.TrackEvent(evt);

            PatrolReward.ClaimReward(null);
        }

        // subscribe from PatrolReward.PatrolTime, Claiming
        private void SetReceiveButton(TimeSpan remainTime, bool claiming)
        {
            var canReceive = remainTime <= TimeSpan.Zero;
            receiveButton.Interactable = canReceive && !claiming;
            receiveButton.Text = canReceive
                ? L10nManager.Localize("UI_GET_REWARD")
                : L10nManager.Localize("UI_REMAINING_TIME", PatrolRewardModule.TimeSpanToString(remainTime));
        }
    }
}
