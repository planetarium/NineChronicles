using CommandLine;
using Cysharp.Threading.Tasks;
using Nekoyume.L10n;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using System;

namespace Nekoyume.UI.Module
{
    using Nekoyume.Action.AdventureBoss;
    using Nekoyume.UI.Model;
    using UniRx;
    public class WoldMapAdventureBossReward : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI[] RemainingBlockIndexs;

        [SerializeField]
        private GameObject LoadingObj;

        private readonly List<System.IDisposable> _disposablesByEnable = new();
        private readonly List<System.IDisposable> _disposablesByInstantiate = new();
        private long _remainingBlockIndex = 0;
        private long _lastClaimedBlockIndex = 0;

        private void Awake()
        {
            Game.Game.instance.AdventureBossData.CurrentState.Subscribe(OnAdventureBossStateChanged).AddTo(_disposablesByInstantiate);
        }

        private void OnDestroy()
        {
            _disposablesByInstantiate.DisposeAllAndClear();
        }

        private void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(UpdateViewAsync)
                .AddTo(_disposablesByEnable);
            Game.Game.instance.AdventureBossData.IsRewardLoading
                .Subscribe(isLoading => {
                    LoadingObj.SetActive(isLoading);
                    foreach (var text in RemainingBlockIndexs)
                    {
                        text.text = "-";
                    }
                })
                .AddTo(_disposablesByEnable);
        }

        private void OnDisable()
        {
            _disposablesByEnable.DisposeAllAndClear();
        }

        private void UpdateViewAsync(long blockIndex)
        {
            if(LoadingObj.activeSelf)
            {
                foreach (var text in RemainingBlockIndexs)
                {
                    text.text = "-";
                }
                return;
            }

            if (_lastClaimedBlockIndex <= blockIndex)
            {
                RefreshLastClaimedBlockIndex(Game.Game.instance.AdventureBossData);
            }

            _remainingBlockIndex = _lastClaimedBlockIndex - blockIndex;
            var timeText = $"{_remainingBlockIndex:#,0}({_remainingBlockIndex.BlockRangeToTimeSpanString()})";
            foreach (var text in RemainingBlockIndexs)
            {
                text.text = timeText;
            }
        }

        private void OnAdventureBossStateChanged(AdventureBossData.AdventureBossSeasonState state)
        {
            var adventureBossData = Game.Game.instance.AdventureBossData;

            //todo 보상 수령가능여부 추가처리진행해야함.
            gameObject.SetActive(adventureBossData.EndedSeasonInfos.Count() != 0);

            RefreshLastClaimedBlockIndex(adventureBossData);
        }

        private void RefreshLastClaimedBlockIndex(AdventureBossData adventureBossData)
        {
            var lastClaimableSeasonInfo = adventureBossData.EndedSeasonInfos
                .Where(info => info.Value.EndBlockIndex + ClaimAdventureBossReward.ClaimableDuration > Game.Game.instance.Agent.BlockIndex)
                .OrderByDescending(info => info.Value.EndBlockIndex) // EndBlockIndex가 큰 순서대로 정렬
                .FirstOrDefault(); // 첫 번째 요소를 선택

            _lastClaimedBlockIndex = lastClaimableSeasonInfo.Value.EndBlockIndex + ClaimAdventureBossReward.ClaimableDuration;
        }

        public void OnClickRewardPopup()
        {
            Widget.Find<AdventureBossRewardPopup>().Show();
        }
    }
}
