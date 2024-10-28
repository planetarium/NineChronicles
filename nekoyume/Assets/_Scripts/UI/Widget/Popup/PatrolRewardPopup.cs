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

        public PatrolReward PatrolReward => patrolRewardModule.PatrolReward;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
            });
            CloseWidget = () => Close(true);

            receiveButton.OnSubmitSubject
                .Subscribe(_ => ClaimReward())
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (PatrolReward.Claiming.Value)
            {
                return;
            }

            var clientInitialized = ApiClients.Instance.PatrolRewardServiceClient.IsInitialized;
            if (!clientInitialized)
            {
                NcDebug.Log(
                    $"[{nameof(PatrolRewardPopup)}]PatrolRewardServiceClient is not initialized.");
                return;
            }

            // PatrolReward.InitializeInformation() 이후에 호출되어야 함. SetData 안에 넣어야 할수도
            var patrolTime = PatrolReward.PatrolTime.Value;
            Analyzer.Instance.Track("Unity/PatrolReward/Show Popup", new Dictionary<string, Value>
            {
                ["PatrolTime"] = patrolTime,
            });

            var evt = new AirbridgeEvent("PatrolReward_Show_Popup");
            evt.AddCustomAttribute("patrol-time", patrolTime.ToString());
            AirbridgeUnity.TrackEvent(evt);

            patrolRewardModule.SetData();

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

        // subscribe from PatrolReward.PatrolTime, Claiming
        private void SetReceiveButton(TimeSpan remainTime, bool claiming)
        {
            var canReceive = remainTime <= TimeSpan.Zero;
            receiveButton.Interactable = canReceive && !claiming;
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
