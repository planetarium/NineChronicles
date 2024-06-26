using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Arena;
using Nekoyume.Game.LiveAsset;
using Nekoyume.GraphQL;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.State;
using Nekoyume.UI.Model;
using UnityEngine;
using static Lib9c.SerializeKeys;

namespace Nekoyume.State
{
    using Libplanet.Common;
    using System.Security.Cryptography;
    using UniRx;

    public static partial class RxProps
    {
        // TODO!!!! Remove [`_arenaInfoTuple`] and use [`_playersArenaParticipant`] instead.
        private static readonly
            AsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>
            _arenaInfoTuple = new(UpdateArenaInfoTupleAsync);

        private static long _arenaInfoTupleUpdatedBlockIndex;

        private static readonly ReactiveProperty<ArenaParticipantModel> _playerArenaInfo =
            new(null);

        private static readonly ReactiveProperty<int> _purchasedDuringInterval = new();

        public static IReadOnlyReactiveProperty<int> PurchasedDuringInterval =>
            _purchasedDuringInterval;
        private static readonly ReactiveProperty<long> _lastArenaBattleBlockIndex = new();

        public static IReadOnlyReactiveProperty<long> LastArenaBattleBlockIndex =>
            _lastArenaBattleBlockIndex;
        public static IReadOnlyReactiveProperty<ArenaParticipantModel> PlayerArenaInfo =>
            _playerArenaInfo;
        public static
            IReadOnlyAsyncUpdatableRxProp<(ArenaInformation current, ArenaInformation next)>
            ArenaInfoTuple => _arenaInfoTuple;

        private static readonly ReactiveProperty<ArenaTicketProgress>
            _arenaTicketsProgress = new(new ArenaTicketProgress());

        public static IReadOnlyReactiveProperty<ArenaTicketProgress>
            ArenaTicketsProgress => _arenaTicketsProgress;

        private static long _arenaParticipantsOrderedWithScoreUpdatedBlockIndex;

        private static readonly AsyncUpdatableRxProp<List<ArenaParticipantModel>>
            _arenaInformationOrderedWithScore = new(
                new List<ArenaParticipantModel>(),
                UpdateArenaInformationOrderedWithScoreAsync);

        public static IReadOnlyAsyncUpdatableRxProp<List<ArenaParticipantModel>>
            ArenaInformationOrderedWithScore => _arenaInformationOrderedWithScore;

        public static void UpdateArenaInfoToNext()
        {
            _arenaInfoTuple.Value = (_arenaInfoTuple.Value.next, null);
        }

        private static void StartArena()
        {
            OnBlockIndexArena(_agent.BlockIndex);
            OnAvatarChangedArena();

            ArenaInfoTuple
                .Subscribe(_ => UpdateArenaTicketProgress(_agent.BlockIndex))
                .AddTo(_disposables);

            PlayerArenaInfo
                .Subscribe(_ => UpdateArenaTicketProgress(_agent.BlockIndex))
                .AddTo(_disposables);
        }

        private static void OnBlockIndexArena(long blockIndex)
        {
            UpdateArenaTicketProgress(blockIndex);
        }

        private static void OnAvatarChangedArena()
        {
            // NOTE: Reset all of cached block indexes for rx props when current avatar state changed.
            _arenaInfoTupleUpdatedBlockIndex = 0;
            _arenaParticipantsOrderedWithScoreUpdatedBlockIndex = 0;

            // TODO!!!! Update [`_playersArenaParticipant`] when current avatar changed.
            // if (_playersArenaParticipant.HasValue &&
            //     _playersArenaParticipant.Value.AvatarAddr == addr)
            // {
            //     return;
            // }
            //
            // _playersArenaParticipant.Value = null;
        }

        private static void UpdateArenaTicketProgress(long blockIndex)
        {
            const int maxTicketCount = ArenaInformation.MaxTicketCount;
            var ticketResetInterval = States.Instance.GameConfigState.DailyArenaInterval;
            var currentArenaInfo = _arenaInfoTuple.HasValue
                ? _arenaInfoTuple.Value.current
                : null;
            if (currentArenaInfo is null)
            {
                _arenaTicketsProgress.Value.Reset(maxTicketCount, maxTicketCount);
                _arenaTicketsProgress.SetValueAndForceNotify(_arenaTicketsProgress.Value);
                return;
            }

            var currentRoundData = _tableSheets.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            var currentTicketCount = currentArenaInfo.GetTicketCount(
                blockIndex,
                currentRoundData.StartBlockIndex,
                ticketResetInterval);
            var purchasedCount = PurchasedDuringInterval.Value;
            var purchasedCountDuringInterval =  currentArenaInfo.GetPurchasedCountInInterval(
                blockIndex,
                currentRoundData.StartBlockIndex,
                ticketResetInterval,
                purchasedCount);
            var progressedBlockRange =
                (blockIndex - currentRoundData.StartBlockIndex) % ticketResetInterval;
            _arenaTicketsProgress.Value.Reset(
                currentTicketCount,
                maxTicketCount,
                (int)progressedBlockRange,
                ticketResetInterval,
                purchasedCountDuringInterval);
            _arenaTicketsProgress.SetValueAndForceNotify(
                _arenaTicketsProgress.Value);
        }

        private static async Task<(ArenaInformation current, ArenaInformation next)>
            UpdateArenaInfoTupleAsync(
                (ArenaInformation current, ArenaInformation next) previous, HashDigest<SHA256> stateRootHash)
        {
            var avatarAddress = _states.CurrentAvatarState?.address;
            if (!avatarAddress.HasValue)
            {
                return (null, null);
            }

            if (_arenaInfoTupleUpdatedBlockIndex == _agent.BlockIndex)
            {
                return previous;
            }

            _arenaInfoTupleUpdatedBlockIndex = _agent.BlockIndex;

            var blockIndex = _agent.BlockIndex;
            var sheet = _tableSheets.ArenaSheet;
            if (!sheet.TryGetCurrentRound(blockIndex, out var currentRoundData))
            {
                NcDebug.Log($"Failed to get current round data. block index({blockIndex})");
                return previous;
            }

            var currentArenaInfoAddress = ArenaInformation.DeriveAddress(
                avatarAddress.Value,
                currentRoundData.ChampionshipId,
                currentRoundData.Round);
            var nextArenaInfoAddress = sheet.TryGetNextRound(blockIndex, out var nextRoundData)
                ? ArenaInformation.DeriveAddress(
                    avatarAddress.Value,
                    nextRoundData.ChampionshipId,
                    nextRoundData.Round)
                : default;
            var dict = await _agent.GetStateBulkAsync(
                stateRootHash,
                ReservedAddresses.LegacyAccount,
                new[]
                {
                    currentArenaInfoAddress,
                    nextArenaInfoAddress
                }
            );
            var currentArenaInfo =
                dict[currentArenaInfoAddress] is List currentList
                    ? new ArenaInformation(currentList)
                    : null;
            var nextArenaInfo =
                dict[nextArenaInfoAddress] is List nextList
                    ? new ArenaInformation(nextList)
                    : null;
            return (currentArenaInfo, nextArenaInfo);
        }

        private static async Task<List<ArenaParticipantModel>>
            UpdateArenaInformationOrderedWithScoreAsync(
            List<ArenaParticipantModel> previous, HashDigest<SHA256> stateRootHash)
        {
            var avatarAddress = _states.CurrentAvatarState?.address;
            List<ArenaParticipantModel> avatarAddrAndScoresWithRank =
                new List<ArenaParticipantModel>();
            if (!avatarAddress.HasValue)
            {
                // TODO!!!!
                // [`States.CurrentAvatarState`]가 바뀔 때, 목록에 추가 정보를 업데이트 한다.
                return avatarAddrAndScoresWithRank;
            }

            if (_arenaParticipantsOrderedWithScoreUpdatedBlockIndex == _agent.BlockIndex)
            {
                return previous;
            }

            _arenaParticipantsOrderedWithScoreUpdatedBlockIndex = _agent.BlockIndex;

            var currentAvatar = _states.CurrentAvatarState;
            var currentAvatarAddr = currentAvatar.address;
            var arenaInfo = new List<ArenaParticipantModel>();
            var agent = Game.Game.instance.Agent;
            var blockIndex = agent.BlockIndex;
            var currentRoundData = Game.Game.instance.TableSheets.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            var playerArenaInfoAddr = ArenaInformation.DeriveAddress(
                currentAvatarAddr,
                currentRoundData.ChampionshipId,
                currentRoundData.Round);
            var purchasedCountAddress =
                playerArenaInfoAddr.Derive(BattleArena.PurchasedCountKey);
            var arenaAvatarAddress =
                ArenaAvatarState.DeriveAddress(currentAvatarAddr);
            var playerScoreAddr = ArenaScore.DeriveAddress(currentAvatarAddr,
                currentRoundData.ChampionshipId,
                currentRoundData.Round);
            var addrBulk = new List<Address>
            {
                purchasedCountAddress,
                arenaAvatarAddress,
                playerScoreAddr
            };
            var stateBulk =
                await agent.GetStateBulkAsync(stateRootHash, ReservedAddresses.LegacyAccount, addrBulk);
            var purchasedCountDuringInterval = stateBulk[purchasedCountAddress] is Integer iValue
                ? (int)iValue
                : 0;
            var arenaAvatarState = stateBulk[arenaAvatarAddress] is List iValue2
                ? new ArenaAvatarState(iValue2)
                : null;
            long lastBattleBlockIndex = arenaAvatarState?.LastBattleBlockIndex ?? 0L;
            try
            {
                var response = await Game.Game.instance.RpcGraphQLClient.QueryArenaInfoAsync(currentAvatarAddr);
                // Arrange my information so that it comes first when it's the same score.
                arenaInfo = response.StateQuery.ArenaParticipants
                    .ToList();
            }
            catch (Exception e)
            {
                NcDebug.LogException(e);
                // TODO: this is temporary code for local testing.
                arenaInfo.AddRange(_states.AvatarStates.Values.Select(avatar => new ArenaParticipantModel
                {
                    AvatarAddr = avatar.address,
                    NameWithHash = avatar.NameWithHash,
                }));
            }

            string playerGuildName = null;
            if (Game.Game.instance.GuildModels.Any())
            {
                var guildModels = Game.Game.instance.GuildModels;
                foreach (var guildModel in guildModels)
                {
                    if (guildModel.AvatarModels.Any(a => a.AvatarAddress == currentAvatarAddr))
                    {
                        playerGuildName = guildModel.Name;
                    }
                    foreach (var info in arenaInfo)
                    {
                        var model = guildModel.AvatarModels.FirstOrDefault(a => a.AvatarAddress == info.AvatarAddr);
                        if (model is not null)
                        {
                            info.GuildName = guildModel.Name;
                        }
                    }
                }
            }

            var portraitId = Util.GetPortraitId(BattleType.Arena);
            var cp = Util.TotalCP(BattleType.Arena);
            ArenaParticipantModel playerArenaInf = new ArenaParticipantModel
            {
                AvatarAddr = currentAvatarAddr,
                Score = ArenaScore.ArenaScoreDefault,
                WinScore = 0,
                LoseScore = 0,
                NameWithHash = currentAvatar.NameWithHash,
                PortraitId = portraitId,
                Cp = cp,
                Level = currentAvatar.level,
                GuildName = playerGuildName,
            };

            if (!arenaInfo.Any())
            {
                NcDebug.Log($"Failed to get {nameof(ArenaParticipantModel)}");

                // TODO!!!! [`_playersArenaParticipant`]를 이 문맥이 아닌 곳에서
                // 따로 처리합니다.
                _playerArenaInfo.SetValueAndForceNotify(playerArenaInf);
                return avatarAddrAndScoresWithRank;
            }

            var playerArenaInfo = arenaInfo.FirstOrDefault(p => p.AvatarAddr == currentAvatarAddr);
            if (playerArenaInfo is null)
            {
                var maxRank = arenaInfo.Max(r => r.Rank);
                var firstMaxRankIndex = arenaInfo.FindIndex(info => info.Rank == maxRank);
                playerArenaInf.Rank = maxRank;
                playerArenaInfo = playerArenaInf;
                arenaInfo.Insert(firstMaxRankIndex, playerArenaInfo);
            }
            else
            {
                playerArenaInfo.Cp = cp;
                playerArenaInfo.PortraitId = portraitId;
                var playerScoreValue = ((List)stateBulk[playerScoreAddr])?[1];
                playerArenaInfo.Score = playerScoreValue == null ? 0 : (Integer)playerScoreValue;
            }

            // NOTE: If the [`addrBulk`] is too large, and split and get separately.
            _purchasedDuringInterval.SetValueAndForceNotify(purchasedCountDuringInterval);
            _lastArenaBattleBlockIndex.SetValueAndForceNotify(lastBattleBlockIndex);

            // Calculate and Reset Rank.
            var arenaInfoList = arenaInfo
                .OrderByDescending(participant => participant.Score)
                .ThenByDescending(participant => participant.AvatarAddr == currentAvatarAddr)
                .ToList();
            var playerIndex =
                arenaInfoList.FindIndex(model => model.AvatarAddr == currentAvatarAddr);
            var avatarCount = arenaInfoList.Count;
            if (playerIndex == 0)
            {
                playerArenaInfo.Rank = 1;
            }
            else if (playerIndex < avatarCount - 1) // avoid out of range exception
            {
                // 다음 순서에 위치한 유저가 같은 점수일 경우, 같은 등수로 표기한다.
                // 같지 않을 경우, 앞 순서에 있는 유저의 등수 - 1로 표기한다.
                playerArenaInfo.Rank =
                    arenaInfoList[playerIndex + 1].Score == playerArenaInfo.Score
                        ? arenaInfoList[playerIndex + 1].Rank
                        : arenaInfoList[playerIndex - 1].Rank + 1;
            }

            SetArenaInfoOnMainThreadAsync(playerArenaInfo).Forget();
            return arenaInfoList;
        }

        private static async UniTask SetArenaInfoOnMainThreadAsync(ArenaParticipantModel playerArenaInfo)
        {
            await UniTask.SwitchToMainThread();
            _playerArenaInfo.SetValueAndForceNotify(playerArenaInfo);
        }
    }
}
