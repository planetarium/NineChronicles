using Cysharp.Threading.Tasks;
using Nekoyume.UI;
using Nekoyume.UI.Module;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.UI
{
    using Nekoyume.Helper;
    using Nekoyume.Model.AdventureBoss;
    using Nekoyume.TableData;
    using System.Linq;
    using TMPro;
    using UniRx;
    using UnityEngine.UI;

    public class AdventureBossRewardInfoPopup : PopupWidget
    {
        [SerializeField] private UnityEngine.UI.ToggleGroup toggleGroup;
        [SerializeField] private Toggle toggleScore;
        [SerializeField] private Toggle toggleFloor;
        [SerializeField] private Toggle toggleOperational;
        [SerializeField] private GameObject contentsScore;
        [SerializeField] private GameObject contentsFloor;
        [SerializeField] private GameObject contentsOperational;
        [SerializeField] private TextMeshProUGUI remainingBlockTime;

        [Header("Score Contents")]
        [SerializeField] private TextMeshProUGUI totalScore;
        [SerializeField] private TextMeshProUGUI myScore;
        [SerializeField] private BaseItemView[] baseItemViews;

        [Header("Floor Contents")]
        [SerializeField] private FloorRewardCell[] floorRewardCells;

        [Header("Operational Contents")]
        [SerializeField] private Image currentSeasonBossImg;
        [SerializeField] private TextMeshProUGUI currentSeasonBossName;
        [SerializeField] private BaseItemView[] currentSeasonBossRewardViews;
        [SerializeField] private BossRewardCell[] bossRewardCells;

        private readonly List<System.IDisposable> _disposablesByEnable = new();
        private long _seasonEndBlock;

        override protected void Awake()
        {
            toggleScore.onValueChanged.AddListener((isOn) =>
            {
                var adventureBossData = Game.Game.instance.AdventureBossData;
                totalScore.text = adventureBossData.ExploreBoard.Value.TotalPoint.ToString("#,0");
                var contribution = (long)adventureBossData.ExploreInfo.Value.Score / adventureBossData.ExploreBoard.Value.TotalPoint;
                myScore.text = $"{adventureBossData.ExploreInfo.Value.Score.ToString("#,0")} ({contribution.ToString("F2")}%)";
                var myReward = new ClaimableReward
                {
                    NcgReward = null,
                    ItemReward = new Dictionary<int, int>(),
                    FavReward = new Dictionary<int, int>(),
                };
                myReward = AdventureBossHelper.CalculateExploreReward(myReward,
                                    adventureBossData.BountyBoard.Value,
                                    adventureBossData.ExploreBoard.Value,
                                    adventureBossData.ExploreInfo.Value,
                                    adventureBossData.ExploreInfo.Value.AvatarAddress,
                                    out var ncgReward);
                int i = 0;
                foreach (var item in myReward.ItemReward)
                {
                    baseItemViews[i].ItemViewSetItemData(item.Key, item.Value);
                    i++;
                }
                foreach(var fav in myReward.FavReward)
                {
                    RuneSheet runeSheet = Game.Game.instance.TableSheets.RuneSheet;
                    runeSheet.TryGetValue(fav.Key, out var runeRow);
                    if (runeRow != null)
                    {
                        baseItemViews[i].ItemViewSetCurrencyData(runeRow.Ticker, fav.Value);
                        i++;
                    }
                }
                for (; i < baseItemViews.Length; i++)
                {
                    baseItemViews[i].gameObject.SetActive(false);
                }

                contentsScore.SetActive(isOn);
            });
            toggleFloor.onValueChanged.AddListener((isOn) =>
            {
                contentsFloor.SetActive(isOn);
            });
            toggleOperational.onValueChanged.AddListener((isOn) =>
            {
                contentsOperational.SetActive(isOn);
            }); 
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Game.Game.instance.AdventureBossData.SeasonInfo.
                Subscribe(RefreshSeasonInfo).
                AddTo(_disposablesByEnable);
            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(UpdateViewAsync)
                .AddTo(_disposablesByEnable);
            base.Show(ignoreShowAnimation);
        }

        private void RefreshSeasonInfo(SeasonInfo seasonInfo)
        {
            if (seasonInfo == null)
            {
                return;
            }
            _seasonEndBlock = seasonInfo.EndBlockIndex;
        }

        private void UpdateViewAsync(long blockIndex)
        {
            var remainingBlockIndex = _seasonEndBlock - blockIndex;
            if (remainingBlockIndex < 0)
            {
                Close();
                return;
            }
            remainingBlockTime.text = $"{remainingBlockIndex:#,0}({remainingBlockIndex.BlockRangeToTimeSpanString()})";
        }
    }
}
