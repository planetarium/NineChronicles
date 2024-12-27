using System;
using System.Collections.Generic;
using mixpanel;
using Nekoyume.ApiClient;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class PatrolRewardPopup : PopupWidget
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private PatrolRewardModule patrolRewardModule;
        [SerializeField] private ConditionalButton receiveButton;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
            });
            CloseWidget = () => Close(true);

            PatrolReward.PatrolTime
                .Where(_ => !PatrolReward.Claiming.Value)
                .Select(patrolTime =>
                {
                    var patrolTimeWithOutSeconds = new TimeSpan(patrolTime.Ticks /
                        TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute);
                    return PatrolReward.Interval - patrolTimeWithOutSeconds;
                })
                .Subscribe(SetReceiveButton)
                .AddTo(gameObject);

            PatrolReward.Claiming.Where(claiming => claiming)
                .Subscribe(_ => receiveButton.Interactable = false)
                .AddTo(gameObject);

            receiveButton.OnSubmitSubject
                .Subscribe(_ => ClaimReward())
                .AddTo(gameObject);
        }

        public void Show(bool ignoreShowAnimation = false)
        {
            if (PatrolReward.Claiming.Value)
            {
                return;
            }

            var clientInitialized = true;
            if (!clientInitialized)
            {
                NcDebug.Log(
                    $"[{nameof(PatrolRewardPopup)}]PatrolRewardServiceClient is not initialized.");
                return;
            }

            ShowAsync(ignoreShowAnimation);
        }

        private async void ShowAsync(bool ignoreShowAnimation = false)
        {
            await patrolRewardModule.SetData();

            var patrolTime = PatrolReward.PatrolTime.Value;
            Analyzer.Instance.Track("Unity/PatrolReward/Show Popup", new Dictionary<string, Value>
            {
                ["PatrolTime"] = patrolTime,
            });

            var evt = new AirbridgeEvent("PatrolReward_Show_Popup");
            evt.AddCustomAttribute("patrol-time", patrolTime.ToString());
            AirbridgeUnity.TrackEvent(evt);

            base.Show(ignoreShowAnimation);
        }

        private void ClaimReward()
        {
            Analyzer.Instance.Track("Unity/PatrolReward/Request Claim Reward");

            var evt = new AirbridgeEvent("PatrolReward_Request_Claim_Reward");
            AirbridgeUnity.TrackEvent(evt);

            PatrolReward.ClaimReward(() => Find<LobbyMenu>().PatrolRewardMenu.ClaimRewardAnimation());
            Close();
        }

        // subscribe from PatrolReward.PatrolTime
        private void SetReceiveButton(TimeSpan remainTime)
        {
            var canReceive = remainTime <= TimeSpan.Zero;
            receiveButton.Interactable = canReceive;
            receiveButton.Text = canReceive
                ? L10nManager.Localize("UI_GET_REWARD")
                : L10nManager.Localize("UI_REMAINING_TIME", PatrolRewardModule.TimeSpanToString(remainTime));
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickClaimPatrolRewardButton()
        {
            receiveButton.OnSubmitSubject.OnNext(default);
        }
    }
}
