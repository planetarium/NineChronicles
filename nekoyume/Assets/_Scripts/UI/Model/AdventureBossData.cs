using System.Collections.Generic;
using Nekoyume.Model.AdventureBoss;
using Cysharp.Threading.Tasks;
using Libplanet.Types.Assets;
using Nekoyume.State;
using Nekoyume.Helper;
using static Nekoyume.Data.AdventureBossGameData;
using Nekoyume.Game;
using Nekoyume;
using Nekoyume.TableData.AdventureBoss;
using System.Linq;
using Nekoyume.L10n;
using UnityEngine;
using Nekoyume.Game.LiveAsset;
using System;

namespace Nekoyume.UI.Model
{
    using Libplanet.Common;
    using System.Security.Cryptography;
    using UniRx;
    public class AdventureBossData
    {
        public enum AdventureBossSeasonState
        {
            None,
            Ready,
            Progress,
            End
        }

        public ReactiveProperty<SeasonInfo> SeasonInfo = new ReactiveProperty<SeasonInfo>();
        public ReactiveProperty<BountyBoard> BountyBoard = new ReactiveProperty<BountyBoard>();
        public ReactiveProperty<ExploreBoard> ExploreBoard = new ReactiveProperty<ExploreBoard>();
        public ReactiveProperty<Explorer> ExploreInfo = new ReactiveProperty<Explorer>();

        public ReactiveProperty<AdventureBossSeasonState> CurrentState = new ReactiveProperty<AdventureBossSeasonState>();
        public ReactiveProperty<bool> IsRewardLoading = new ReactiveProperty<bool>();

        public Dictionary<long, SeasonInfo> EndedSeasonInfos = new Dictionary<long, SeasonInfo>();
        public Dictionary<long, BountyBoard> EndedBountyBoards = new Dictionary<long, BountyBoard>();
        public Dictionary<long, ExploreBoard> EndedExploreBoards = new Dictionary<long, ExploreBoard>();
        public Dictionary<long, Explorer> EndedExploreInfos = new Dictionary<long, Explorer>();

        private const int _endedSeasonSearchTryCount = 10;

        public void Initialize()
        {
            IsRewardLoading.Value = false;
            RefreshAllByCurrentState().Forget();
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe((System.Action<long>)(blockIndex =>
            {
                if (this.SeasonInfo.Value == null)
                {
                    return;
                }

                if (this.SeasonInfo.Value.EndBlockIndex == blockIndex ||
                    this.SeasonInfo.Value.NextStartBlockIndex == blockIndex)
                {
                    RefreshAllByCurrentState().Forget();
                }
            }));
        }

        public async UniTask RefreshEndedSeasons(HashDigest<SHA256>? stateRootHash = null, long blockIndex = 0)
        {
            //최근시즌이 종료된경우 끝난시즌정보에 추가.
            int startIndex = SeasonInfo.Value.EndBlockIndex < Game.Game.instance.Agent.BlockIndex ? 0 : 1;

            //최대 10개의 종료된 시즌정보를 가져온다.(만일을 대비 해서)
            for (int i = startIndex; i < _endedSeasonSearchTryCount; i++)
            {
                var endedSeasonIndex = SeasonInfo.Value.Season - i;

                //최초시즌이 0이하로 내려가면 더이상 찾지않음.
                if (endedSeasonIndex <= 0)
                    break;

                var endedSeasonInfo = await Game.Game.instance.Agent.GetAdventureBossSeasonInfoAsync(endedSeasonIndex, stateRootHash, blockIndex);
                var endedBountyBoard = await Game.Game.instance.Agent.GetBountyBoardAsync(endedSeasonIndex, stateRootHash, blockIndex);
                var endedExploreBoard = await Game.Game.instance.Agent.GetExploreBoardAsync(endedSeasonIndex, stateRootHash, blockIndex);

                if (!EndedSeasonInfos.ContainsKey(endedSeasonIndex))
                {
                    EndedSeasonInfos.Add(endedSeasonIndex, endedSeasonInfo);
                    EndedBountyBoards.Add(endedSeasonIndex, endedBountyBoard);
                    EndedExploreBoards.Add(endedSeasonIndex, endedExploreBoard);
                }
                else
                {
                    EndedSeasonInfos[endedSeasonIndex] = endedSeasonInfo;
                    EndedBountyBoards[endedSeasonIndex] = endedBountyBoard;
                    EndedExploreBoards[endedSeasonIndex] = endedExploreBoard;
                }

                if (States.Instance.CurrentAvatarState.address != null)
                {
                    var endedExploreInfo = await Game.Game.instance.Agent.GetExploreInfoAsync(States.Instance.CurrentAvatarState.address, endedSeasonIndex, stateRootHash, blockIndex);
                    if (!EndedExploreInfos.ContainsKey(endedSeasonIndex))
                    {
                        EndedExploreInfos.Add(endedSeasonIndex, endedExploreInfo);
                    }
                    else
                    {
                        EndedExploreInfos[endedSeasonIndex] = endedExploreInfo;
                    }
                }

                //보상수령기간이 지날경우 더이상 가져오지않음.
                if (endedSeasonInfo.EndBlockIndex + State.States.Instance.GameConfigState.AdventureBossClaimInterval < Game.Game.instance.Agent.BlockIndex)
                {
                    break;
                }
            }
        }

        public async UniTask RefreshAllByCurrentState(HashDigest<SHA256>? stateRootHash = null, long blockIndex = 0)
        {
            SeasonInfo.Value = await Game.Game.instance.Agent.GetAdventureBossLatestSeasonAsync(stateRootHash, blockIndex);

            //알림 등록
            if (SeasonInfo.Value != null && SeasonInfo.Value.EndBlockIndex != 0)
            {
                try
                {
                    int secondsPerBlock = LiveAssetManager.instance.GameConfig.SecondsPerBlock;
                    double blocksPerHour = 3600.0 / secondsPerBlock;
                    int roundedBlocksPerHour = (int)Math.Round(blocksPerHour);
                    ReserveAdventureBossSeasonPush("PUSH_ADVENTURE_BOSS_SEASON_END_HOUR_AGO_CONTENT", SeasonInfo.Value.Season, SeasonInfo.Value.EndBlockIndex - roundedBlocksPerHour);
                }
                catch (System.Exception e)
                {
                    NcDebug.LogError($"[AdventureBossData]{e}");
                }

                ReserveAdventureBossSeasonPush("PUSH_ADVENTURE_BOSS_SEASON_END_CONTENT", SeasonInfo.Value.Season, SeasonInfo.Value.EndBlockIndex);
                ReserveAdventureBossSeasonPush("PUSH_ADVENTURE_BOSS_SEASON_START_CONTENT", SeasonInfo.Value.Season+1, SeasonInfo.Value.NextStartBlockIndex);
            }

            //최근시즌이 종료된경우 끝난시즌정보에 추가.
            int startIndex = SeasonInfo.Value.EndBlockIndex < Game.Game.instance.Agent.BlockIndex ? 0 : 1;

            //최대 10개의 종료된 시즌정보를 가져온다.(만일을 대비 해서)
            for (int i = startIndex; i < _endedSeasonSearchTryCount; i++)
            {
                var endedSeasonIndex = SeasonInfo.Value.Season - i;

                //최초시즌이 0이하로 내려가면 더이상 찾지않음.
                if (endedSeasonIndex <= 0)
                    break;

                if (!EndedExploreInfos.TryGetValue(endedSeasonIndex, out var endedExploreInfo) && Game.Game.instance.States.CurrentAvatarState != null)
                {
                    endedExploreInfo = await Game.Game.instance.Agent.GetExploreInfoAsync(States.Instance.CurrentAvatarState.address, endedSeasonIndex, stateRootHash, blockIndex);
                    EndedExploreInfos.Add(endedSeasonIndex, endedExploreInfo);
                }

                //이미 가져온 시즌정보는 다시 가져오지않음.
                if (!EndedSeasonInfos.TryGetValue(endedSeasonIndex, out var endedSeasonInfo))
                {
                    endedSeasonInfo = await Game.Game.instance.Agent.GetAdventureBossSeasonInfoAsync(endedSeasonIndex, stateRootHash, blockIndex);
                    var endedBountyBoard = await Game.Game.instance.Agent.GetBountyBoardAsync(endedSeasonIndex, stateRootHash, blockIndex);
                    var endedExploreBoard = await Game.Game.instance.Agent.GetExploreBoardAsync(endedSeasonIndex, stateRootHash, blockIndex);

                    EndedSeasonInfos.Add(endedSeasonIndex, endedSeasonInfo);
                    EndedBountyBoards.Add(endedSeasonIndex, endedBountyBoard);
                    EndedExploreBoards.Add(endedSeasonIndex, endedExploreBoard);


                    //보상수령기간이 지날경우 더이상 가져오지않음.
                    if (endedSeasonInfo.EndBlockIndex + State.States.Instance.GameConfigState.AdventureBossClaimInterval < Game.Game.instance.Agent.BlockIndex)
                    {
                        break;
                    }
                }

            }

            //시즌이 진행중인 경우.
            if (SeasonInfo.Value.StartBlockIndex <= Game.Game.instance.Agent.BlockIndex && Game.Game.instance.Agent.BlockIndex < SeasonInfo.Value.EndBlockIndex)
            {
                BountyBoard.Value = await Game.Game.instance.Agent.GetBountyBoardAsync(SeasonInfo.Value.Season, stateRootHash, blockIndex);
                ExploreBoard.Value = await Game.Game.instance.Agent.GetExploreBoardAsync(SeasonInfo.Value.Season, stateRootHash, blockIndex);
                if (Game.Game.instance.States.CurrentAvatarState != null)
                {
                    ExploreInfo.Value = await Game.Game.instance.Agent.GetExploreInfoAsync(Game.Game.instance.States.CurrentAvatarState.address, SeasonInfo.Value.Season, stateRootHash, blockIndex);
                }
                CurrentState.Value = AdventureBossSeasonState.Progress;
                NcDebug.Log("[AdventureBossData.RefreshAllByCurrentState] Progress");
                return;
            }

            //시즌시작은되었으나 아무도 현상금을 걸지않아서 시즌데이터가없는경우.
            if (SeasonInfo.Value.NextStartBlockIndex <= Game.Game.instance.Agent.BlockIndex)
            {
                try
                {
                    BountyBoard.Value = null;
                    ExploreInfo.Value = null;
                    ExploreBoard.Value = null;
                }
                catch (System.Exception e)
                {
                    NcDebug.LogError($"[AdventureBossData]{e}");
                }
                try
                {
                    CurrentState.Value = AdventureBossSeasonState.Ready;
                }
                catch (System.Exception e)
                {
                    NcDebug.LogError($"[AdventureBossData]{e}");
                }
                NcDebug.Log("[AdventureBossData.RefreshAllByCurrentState] Ready");
                return;
            }

            //시즌이 종료후 대기중인 경우.
            //종료된 상황인경우 시즌정보 받아서 저장.
            if (SeasonInfo.Value.Season > 0 && !EndedSeasonInfos.ContainsKey(SeasonInfo.Value.Season))
            {
                EndedSeasonInfos.TryAdd(SeasonInfo.Value.Season, SeasonInfo.Value);
                EndedBountyBoards.TryAdd(SeasonInfo.Value.Season, BountyBoard.Value);
                EndedExploreBoards.TryAdd(SeasonInfo.Value.Season, ExploreBoard.Value);
            }
            try
            {
                BountyBoard.Value = null;
                ExploreInfo.Value = null;
                ExploreBoard.Value = null;
                CurrentState.Value = AdventureBossSeasonState.End;
            }
            catch (System.Exception e)
            {
                NcDebug.LogError($"[AdventureBossData]{e}");
            }
            NcDebug.Log("[AdventureBossData.RefreshAllByCurrentState] End");
            return;
        }

        public Investor GetCurrentInvestorInfo()
        {
            if (BountyBoard.Value == null || BountyBoard.Value.Investors == null)
            {
                return null;
            }

            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            return BountyBoard.Value.Investors.Find(i => i.AvatarAddress == avatarAddress);
        }

        public FungibleAssetValue GetCurrentBountyPrice()
        {
            FungibleAssetValue total = new FungibleAssetValue(States.Instance.GoldBalanceState.Gold.Currency, 0, 0);
            if (BountyBoard.Value == null || BountyBoard.Value.Investors == null)
            {
                return total;
            }

            foreach (var item in BountyBoard.Value.Investors)
            {
                total += item.Price;
            }

            return total;
        }

        public bool GetCurrentBossData(out AdventureBossSheet.Row bossData)
        {
            bossData = null;
            if (SeasonInfo.Value == null)
            {
                NcDebug.LogError("SeasonInfo is null");
                return false;
            }
            var tableSheets = TableSheets.Instance;
            bossData = tableSheets.AdventureBossSheet.Values.FirstOrDefault(boss => boss.BossId == SeasonInfo.Value.BossId);
            if (bossData == null)
            {
                NcDebug.LogError($"Not found boss data. bossId: {SeasonInfo.Value.BossId}");
                return false;
            }
            return true;
        }

        public bool GetCurrentUnlockFloorCost(int floor, out AdventureBossUnlockFloorCostSheet.Row unlockFloorCostRow)
        {
            unlockFloorCostRow = null;
            var tableSheets = TableSheets.Instance;
            var seasonInfo = SeasonInfo.Value;
            if (seasonInfo == null)
            {
                NcDebug.LogError("SeasonInfo is null");
                return false;
            }
            var bossData = tableSheets.AdventureBossSheet.Values.FirstOrDefault(boss => boss.BossId == seasonInfo.BossId);
            if (bossData == null)
            {
                NcDebug.LogError($"Not found boss data. bossId: {seasonInfo.BossId}");
                return false;
            }
            var floorRowData = tableSheets.AdventureBossFloorSheet.Values.FirstOrDefault(floorRow => floorRow.AdventureBossId == bossData.Id && floorRow.Floor == floor);
            if (floorRowData == null)
            {
                NcDebug.LogError($"Not found floor data. bossId: {bossData.Id}, floor: {floor}");
                return false;
            }

            if (!tableSheets.AdventureBossUnlockFloorCostSheet.TryGetValue(floorRowData.Id, out unlockFloorCostRow))
            {
                NcDebug.LogError($"Not found unlock cost data. unlockCostId: {floorRowData.Id}");
                return false;
            }
            return true;
        }

        private void ReserveAdventureBossSeasonPush(string l10nKey, long seasonId, long targetBlockIndex)
        {
            var pushidentifierKey = $"{l10nKey}{seasonId}";
            var prevPushIdentifier =
                PlayerPrefs.GetString(pushidentifierKey, string.Empty);
            if (!string.IsNullOrEmpty(prevPushIdentifier))
            {
                PushNotifier.CancelReservation(prevPushIdentifier);
                PlayerPrefs.DeleteKey(pushidentifierKey);
            }

            var timeSpan = (targetBlockIndex - Game.Game.instance.Agent.BlockIndex).BlockToTimeSpan();

            var content = L10nManager.Localize(l10nKey, seasonId);
            var identifier = PushNotifier.Push(content, timeSpan, PushNotifier.PushType.AdventureBoss);
            PlayerPrefs.SetString(pushidentifierKey, identifier);
        }

        public ClaimableReward GetCurrentTotalRewards()
        {
            var myReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };

            if (SeasonInfo.Value == null ||
                BountyBoard.Value == null)
            {
                return myReward;
            }

            try
            {
                if (ExploreBoard.Value != null && ExploreInfo.Value != null)
                {
                    myReward = AdventureBossHelper.CalculateExploreReward(myReward,
                                        BountyBoard.Value,
                                        ExploreBoard.Value,
                                        ExploreInfo.Value,
                                        ExploreInfo.Value.AvatarAddress,
                                        TableSheets.Instance.AdventureBossNcgRewardRatioSheet,
                                        States.Instance.GameConfigState.AdventureBossNcgApRatio,
                                        States.Instance.GameConfigState.AdventureBossNcgRuneRatio,
                                        false,
                                        out var ncgReward);
                }
                else
                {
                    myReward = AdventureBossHelper.CalculateWantedReward(myReward,
                                        BountyBoard.Value,
                                        Game.Game.instance.States.CurrentAvatarState.address,
                                        TableSheets.Instance.AdventureBossNcgRewardRatioSheet,
                                        States.Instance.GameConfigState.AdventureBossNcgRuneRatio,
                                        out var wantedReward);
                }
            }
            catch (System.Exception e)
            {
                NcDebug.LogError($"[AdventureBossData.GetCurrentTotalRewards]{e}");
            }
            return myReward;
        }

        public ClaimableReward GetCurrentBountyRewards()
        {
            var myReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };

            if (SeasonInfo.Value == null || BountyBoard.Value == null || GetCurrentInvestorInfo() == null)
            {
                return myReward;
            }

            myReward = AdventureBossHelper.CalculateWantedReward(myReward,
                                BountyBoard.Value,
                                Game.Game.instance.States.CurrentAvatarState.address,
                                TableSheets.Instance.AdventureBossNcgRewardRatioSheet,
                                States.Instance.GameConfigState.AdventureBossNcgRuneRatio,
                                out var wantedReward);
            return myReward;
        }

        public ClaimableReward GetCurrentBountyRewards(long additionalAmount)
        {
            var myReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };

            if (SeasonInfo.Value == null || BountyBoard.Value == null || GetCurrentInvestorInfo() == null)
            {
                return myReward;
            }
            var fav = new FungibleAssetValue(
                States.Instance.GoldBalanceState.Gold.Currency,
                additionalAmount,
                0);
            var copyedBoad = new BountyBoard((Bencodex.Types.List)BountyBoard.Value.Bencoded);
            var investor = copyedBoad.Investors.Find(i => i.AvatarAddress == Game.Game.instance.States.CurrentAvatarState.address);
            if (investor != null)
            {
                investor.Price += fav;
            }
            else
            {
                copyedBoad.Investors.Add(new Investor(Game.Game.instance.States.CurrentAvatarState.address, Game.Game.instance.States.CurrentAvatarState.name, fav));
            }
            myReward = AdventureBossHelper.CalculateWantedReward(myReward,
                                copyedBoad,
                                Game.Game.instance.States.CurrentAvatarState.address,
                                TableSheets.Instance.AdventureBossNcgRewardRatioSheet,
                                States.Instance.GameConfigState.AdventureBossNcgRuneRatio,
                                out var wantedReward);
            return myReward;
        }

        public ClaimableReward GetCurrentExploreRewards()
        {
            var myReward = new ClaimableReward
            {
                NcgReward = null,
                ItemReward = new Dictionary<int, int>(),
                FavReward = new Dictionary<int, int>(),
            };
            if (ExploreBoard.Value != null && ExploreInfo.Value != null)
            {
                myReward = AdventureBossHelper.CalculateExploreReward(myReward,
                                    BountyBoard.Value,
                                    ExploreBoard.Value,
                                    ExploreInfo.Value,
                                    ExploreInfo.Value.AvatarAddress,
                                    TableSheets.Instance.AdventureBossNcgRewardRatioSheet,
                                    States.Instance.GameConfigState.AdventureBossNcgApRatio,
                                    States.Instance.GameConfigState.AdventureBossNcgRuneRatio,
                                    false,
                                    out var ncgReward);
            }
            return myReward;
        }

        static public ClaimableReward AddClaimableReward(ClaimableReward origin, ClaimableReward addable)
        {
            if (origin.NcgReward == null || origin.NcgReward.Value == null)
            {
                origin.NcgReward = addable.NcgReward;
            }
            else
            {
                origin.NcgReward += addable.NcgReward;
            }
            foreach (var item in addable.ItemReward)
            {
                if (origin.ItemReward.ContainsKey(item.Key))
                {
                    origin.ItemReward[item.Key] += item.Value;
                }
                else
                {
                    origin.ItemReward.Add(item.Key, item.Value);
                }
            }
            foreach (var fav in addable.FavReward)
            {
                if (origin.FavReward.ContainsKey(fav.Key))
                {
                    origin.FavReward[fav.Key] += fav.Value;
                }
                else
                {
                    origin.FavReward.Add(fav.Key, fav.Value);
                }
            }
            return origin;
        }
    }
}
