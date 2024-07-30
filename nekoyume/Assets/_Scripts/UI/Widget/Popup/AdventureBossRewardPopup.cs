using Cysharp.Threading.Tasks;
using Nekoyume.Blockchain;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.State;
using Nekoyume.UI.Module;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using System;

namespace Nekoyume.UI
{
    using Libplanet.Types.Assets;
    using Game;
    using Game.Controller;
    using Helper;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using TableData;
    using Model;
    using System.Reactive.Linq;
    using UniRx;
    using static Data.AdventureBossGameData;

    public class AdventureBossRewardPopup : PopupWidget
    {
        [SerializeField]
        private TextMeshProUGUI bountyCost;

        [SerializeField]
        private TextMeshProUGUI myScore;

        [SerializeField]
        private TextMeshProUGUI myScoreRatioText;

        [SerializeField]
        private GameObject rewardItemsBounty;

        [SerializeField]
        private GameObject rewardItemsExplore;

        [SerializeField]
        private GameObject noRewardItemsBounty;

        [SerializeField]
        private GameObject noRewardItemsExplore;

        [SerializeField]
        private ConditionalButton receiveAllButton;

        [SerializeField]
        private BaseItemView[] rewardItems;

        [SerializeField]
        private BaseItemView[] rewardItemsExplores;

        private List<SeasonInfo> _endedClaimableSeasonInfo = new();
        private long _lastSeasonId = 0;

        private ClaimableReward lastClaimableReward = new()
        {
            NcgReward = null,
            ItemReward = new Dictionary<int, int>(),
            FavReward = new Dictionary<int, int>()
        };

        protected override void Awake()
        {
            receiveAllButton.OnClickSubject.ThrottleFirst(TimeSpan.FromSeconds(1f)).Subscribe(_ =>
            {
                ActionManager.Instance.ClaimAdventureBossReward(_lastSeasonId).Subscribe(eval =>
                {
                    if (eval.Exception != null)
                    {
                        NcDebug.LogError(eval.Exception);
                        OneLineSystem.Push(MailType.System, eval.Exception.InnerException.Message, Scroller.NotificationCell.NotificationType.Alert);
                        Game.instance.AdventureBossData.IsRewardLoading.Value = false;
                        return;
                    }

                    var action = eval.Action;
                    var tableSheets = Game.instance.TableSheets;
                    var mailRewards = new List<MailReward>();

                    if (lastClaimableReward.NcgReward != null && lastClaimableReward.NcgReward.Value != null)
                    {
                        mailRewards.Add(new MailReward(lastClaimableReward.NcgReward.Value, (int)lastClaimableReward.NcgReward.Value.MajorUnit));
                    }

                    foreach (var itemReward in lastClaimableReward.ItemReward)
                    {
                        var itemRow = TableSheets.Instance.ItemSheet[itemReward.Key];
                        if (itemRow is MaterialItemSheet.Row materialRow)
                        {
                            var item = ItemFactory.CreateMaterial(materialRow);
                            mailRewards.Add(new MailReward(item, itemReward.Value));
                        }
                        else
                        {
                            for (var i = 0; i < itemReward.Value; i++)
                            {
                                var item = ItemFactory.CreateItem(itemRow, new ActionRenderHandler.LocalRandom(0));
                                mailRewards.Add(new MailReward(item, 1));
                            }
                        }
                    }

                    foreach (var favReward in lastClaimableReward.FavReward)
                    {
                        var runeSheet = Game.instance.TableSheets.RuneSheet;
                        if (runeSheet.TryGetValue(favReward.Key, out var runeRow))
                        {
                            var currency = Currency.Legacy(runeRow.Ticker, 0, null);
                            var fav = new FungibleAssetValue(currency, favReward.Value, 0);
                            mailRewards.Add(new MailReward(fav, favReward.Value));
                        }
                    }

                    Find<RewardScreen>().Show(mailRewards, "NOTIFICATION_CLAIM_ADVENTURE_BOSS_REWARD_COMPLETE");
                    Game.instance.AdventureBossData.IsRewardLoading.Value = false;
                });
                Game.instance.AdventureBossData.IsRewardLoading.Value = true;
                base.Close();
            }).AddTo(gameObject);
            base.Awake();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (Nekoyume.Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                return;
            }

            var adventureBossData = Game.instance.AdventureBossData;
            if (adventureBossData.EndedSeasonInfos.Count == 0 || adventureBossData.EndedBountyBoards.Count == 0)
            {
                return;
            }

            _endedClaimableSeasonInfo = adventureBossData.EndedSeasonInfos.Values.Where(seasonInfo => seasonInfo.EndBlockIndex + States.Instance.GameConfigState.AdventureBossClaimInterval > Game.instance.Agent.BlockIndex).OrderBy(seasonInfo => seasonInfo.EndBlockIndex).ToList();

            var claimableInfo = adventureBossData.EndedBountyBoards.Values.Where(bountyBoard => _endedClaimableSeasonInfo.Any(seasondata => seasondata.Season == bountyBoard.Season) &&
                bountyBoard.Investors.Any(inv => inv.AvatarAddress == Game.instance.States.CurrentAvatarState.address && !inv.Claimed)).ToList();

            var claimableExploreInfo = adventureBossData.EndedExploreInfos.Join(
                _endedClaimableSeasonInfo,
                exploreInfo => exploreInfo.Key,
                seasonInfo => seasonInfo.Season,
                (exploreInfo, seasonInfo) => exploreInfo).Where(exploreInfo => exploreInfo.Value != null && !exploreInfo.Value.Claimed).ToList();


            if (claimableInfo.Count == 0 && claimableExploreInfo.Count == 0)
            {
                return;
            }

            LoadTotalRewards().ContinueWith((result) =>
            {
                if (result)
                {
                    AudioController.instance.PlaySfx(AudioController.SfxCode.Rewards);
                    base.Show(ignoreShowAnimation);
                }
            });
        }

        public async UniTask<bool> LoadTotalRewards()
        {
            var wantedClaimableReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>()
            };
            var exprolerClaimableReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>()
            };
            lastClaimableReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>()
            };

            foreach (var seasonInfo in _endedClaimableSeasonInfo)
            {
                if (seasonInfo.EndBlockIndex + States.Instance.GameConfigState.AdventureBossClaimInterval <= Game.instance.Agent.BlockIndex)
                {
                    continue;
                }

                var bountyBoard = await Game.instance.Agent.GetBountyBoardAsync(seasonInfo.Season);
                if (Game.instance.AdventureBossData.EndedBountyBoards.ContainsKey(seasonInfo.Season))
                {
                    Game.instance.AdventureBossData.EndedBountyBoards[seasonInfo.Season] = bountyBoard;
                }

                var exploreBoard = await Game.instance.Agent.GetExploreBoardAsync(seasonInfo.Season);
                if (Game.instance.AdventureBossData.EndedExploreBoards.ContainsKey(seasonInfo.Season))
                {
                    Game.instance.AdventureBossData.EndedExploreBoards[seasonInfo.Season] = exploreBoard;
                }

                var exploreInfo = await Game.instance.Agent.GetExploreInfoAsync(States.Instance.CurrentAvatarState.address, seasonInfo.Season);

                var investor = bountyBoard.Investors.FirstOrDefault(
                    inv => inv.AvatarAddress == Game.instance.States.CurrentAvatarState.address);

                try
                {
                    if (investor != null && !investor.Claimed)
                    {
                        wantedClaimableReward = AdventureBossHelper.CalculateWantedReward(wantedClaimableReward,
                            bountyBoard,
                            Game.instance.States.CurrentAvatarState.address,
                            TableSheets.Instance.AdventureBossNcgRewardRatioSheet,
                            States.Instance.GameConfigState.AdventureBossNcgRuneRatio,
                            out var wantedReward);
                        if (_lastSeasonId < seasonInfo.Season)
                        {
                            _lastSeasonId = seasonInfo.Season;
                            RefreshWithSeasonInfo(exploreBoard, exploreInfo, bountyBoard);
                        }
                    }

                    if (exploreInfo != null && exploreInfo.Score > 0 && !exploreInfo.Claimed)
                    {
                        exprolerClaimableReward = AdventureBossHelper.CalculateExploreReward(exprolerClaimableReward,
                            bountyBoard,
                            exploreBoard,
                            exploreInfo,
                            Game.instance.States.CurrentAvatarState.address,
                            TableSheets.Instance.AdventureBossNcgRewardRatioSheet,
                            States.Instance.GameConfigState.AdventureBossNcgApRatio,
                            States.Instance.GameConfigState.AdventureBossNcgRuneRatio,
                            false,
                            out var explorerReward);
                        if (_lastSeasonId < seasonInfo.Season)
                        {
                            _lastSeasonId = seasonInfo.Season;
                            RefreshWithSeasonInfo(exploreBoard, exploreInfo, bountyBoard);
                        }
                    }
                }
                catch (Exception e)
                {
                    NcDebug.LogError(e);
                }
            }

            if (wantedClaimableReward.ItemReward.Count > 0 || wantedClaimableReward.FavReward.Count > 0)
            {
                rewardItemsBounty.SetActive(true);
                noRewardItemsBounty.SetActive(false);
                var i = 0;
                if (wantedClaimableReward.NcgReward != null && wantedClaimableReward.NcgReward.HasValue && !wantedClaimableReward.NcgReward.Value.RawValue.IsZero)
                {
                    rewardItems[i].ItemViewSetCurrencyData(wantedClaimableReward.NcgReward.Value);
                    i++;
                }

                foreach (var itemReward in wantedClaimableReward.ItemReward)
                {
                    if (i > rewardItems.Length)
                    {
                        NcDebug.LogError("rewardItems is not enough");
                        break;
                    }

                    rewardItems[i].ItemViewSetItemData(itemReward.Key, itemReward.Value);
                    i++;
                }

                foreach (var favReward in wantedClaimableReward.FavReward)
                {
                    if (i > rewardItems.Length)
                    {
                        NcDebug.LogError("rewardItems is not enough");
                        break;
                    }

                    rewardItems[i].ItemViewSetCurrencyData(favReward.Key, favReward.Value);
                    i++;
                }

                for (; i < rewardItems.Length; i++)
                {
                    rewardItems[i].gameObject.SetActive(false);
                }
            }
            else
            {
                rewardItemsBounty.SetActive(false);
                noRewardItemsBounty.SetActive(true);
            }

            if (exprolerClaimableReward.ItemReward.Count > 0 || exprolerClaimableReward.FavReward.Count > 0)
            {
                rewardItemsExplore.SetActive(true);
                noRewardItemsExplore.SetActive(false);
                var i = 0;
                if (exprolerClaimableReward.NcgReward != null && exprolerClaimableReward.NcgReward.HasValue && !exprolerClaimableReward.NcgReward.Value.RawValue.IsZero)
                {
                    rewardItemsExplores[i].ItemViewSetCurrencyData(exprolerClaimableReward.NcgReward.Value);
                    i++;
                }

                foreach (var itemReward in exprolerClaimableReward.ItemReward)
                {
                    if (i > rewardItemsExplores.Length)
                    {
                        NcDebug.LogError("rewardItemsExplores is not enough");
                        break;
                    }

                    rewardItemsExplores[i].ItemViewSetItemData(itemReward.Key, itemReward.Value);
                    i++;
                }

                foreach (var favReward in exprolerClaimableReward.FavReward)
                {
                    if (i > rewardItemsExplores.Length)
                    {
                        NcDebug.LogError("rewardItemsExplores is not enough");
                        break;
                    }

                    rewardItemsExplores[i].ItemViewSetCurrencyData(favReward.Key, favReward.Value);
                    i++;
                }

                for (; i < rewardItems.Length; i++)
                {
                    rewardItemsExplores[i].gameObject.SetActive(false);
                }
            }
            else
            {
                rewardItemsExplore.SetActive(false);
                noRewardItemsExplore.SetActive(true);
            }

            lastClaimableReward = AdventureBossData.AddClaimableReward(lastClaimableReward, wantedClaimableReward);
            lastClaimableReward = AdventureBossData.AddClaimableReward(lastClaimableReward, exprolerClaimableReward);

            if (lastClaimableReward.ItemReward.Count == 0 && lastClaimableReward.FavReward.Count == 0 &&
                (lastClaimableReward.NcgReward == null || !lastClaimableReward.NcgReward.HasValue || lastClaimableReward.NcgReward.Value.RawValue.IsZero))
            {
                Game.instance.AdventureBossData.EndedExploreInfos.TryGetValue(_lastSeasonId, out var exploreInfo);
                Game.instance.AdventureBossData.EndedBountyBoards.TryGetValue(_lastSeasonId, out var bountyBoard);
                NcDebug.LogError($"lastClaimableReward is empty {_lastSeasonId} {exploreInfo?.Claimed} {bountyBoard?.Investors.Count} {bountyBoard?.Investors.FirstOrDefault(inv => inv.AvatarAddress == Game.instance.States.CurrentAvatarState.address)?.Claimed}");
                return false;
            }

            return true;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            receiveAllButton.OnClickSubject.OnNext(receiveAllButton.CurrentState.Value);
            base.Close(ignoreCloseAnimation);
        }

        private void RefreshWithSeasonInfo(ExploreBoard exploreBoard, Explorer exploreInfo, BountyBoard bountyBoard)
        {
            bountyCost.text = "-";
            myScore.text = "-";
            myScoreRatioText.text = "-";

            rewardItemsBounty.SetActive(false);
            rewardItemsExplore.SetActive(false);
            noRewardItemsBounty.SetActive(false);
            noRewardItemsExplore.SetActive(false);

            if (exploreInfo != null && exploreBoard != null)
            {
                myScore.text = $"{exploreInfo.Score:#,0}";
                SetExploreInfoVIew(true);
                var myScoreRatio = exploreInfo.Score == 0 ? 0 : (double)exploreInfo.Score / (double)exploreBoard.TotalPoint;
                myScoreRatioText.text = $"{myScoreRatio * 100:F2}%";
            }
            else
            {
                SetExploreInfoVIew(false);
            }

            if (bountyBoard != null)
            {
                var bountyInfo = bountyBoard.Investors.Where(i => i.AvatarAddress.Equals(States.Instance.CurrentAvatarState.address)).FirstOrDefault();
                if (bountyInfo != null)
                {
                    bountyCost.text = $"{bountyInfo.Price.ToCurrencyNotation()}";
                    SetBountyInfoView(true);
                    return;
                }
                else
                {
                    SetBountyInfoView(false);
                }
            }
        }

        private void SetBountyInfoView(bool visible)
        {
            rewardItemsBounty.SetActive(visible);
            noRewardItemsBounty.SetActive(!visible);
        }

        private void SetExploreInfoVIew(bool visible)
        {
            rewardItemsExplore.SetActive(visible);
            noRewardItemsExplore.SetActive(!visible);
        }
    }
}
