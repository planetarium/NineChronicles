using System;
using System.Collections.Generic;
using JetBrains.Annotations;
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

        private bool _isInitialized;

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
                .Select(patrolTime => PatrolReward.Interval - patrolTime)
                .Subscribe(SetReceiveButton)
                .AddTo(gameObject);

            PatrolReward.Claiming.Where(claiming => claiming)
                .Subscribe(_ => receiveButton.Interactable = false)
                .AddTo(gameObject);

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

            if (!_isInitialized)
            {
                patrolRewardModule.Initialize();
                _isInitialized = true;
            }

            patrolRewardModule.SetData();

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

        private void SetReceiveButton(long remainTime)
        {
            var canReceive = remainTime <= 0L;
            receiveButton.Interactable = canReceive;
            receiveButton.Text = canReceive
                ? L10nManager.Localize("UI_GET_REWARD")
                : L10nManager.Localize("UI_REMAINING_TIME", remainTime.BlockRangeToTimeSpanString());
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        [UsedImplicitly]
        public void TutorialActionClickClaimPatrolRewardButton()
        {
            receiveButton.OnSubmitSubject.OnNext(default);
        }
    }
}
