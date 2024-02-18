using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
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
        private static readonly ReactiveProperty<long> _lastBattleBlockIndex = new();

        public static IReadOnlyReactiveProperty<long> LastBattleBlockIndex =>
            _lastBattleBlockIndex;
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
                (ArenaInformation current, ArenaInformation next) previous)
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
                Debug.Log($"Failed to get current round data. block index({blockIndex})");
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
            UpdateArenaInformationOrderedWithScoreAsync(List<ArenaParticipantModel> previous)
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
            var addrBulk = new List<Address>
            {
                purchasedCountAddress,
                arenaAvatarAddress,
            };
            var stateBulk =
                await agent.GetStateBulkAsync(ReservedAddresses.LegacyAccount, addrBulk);
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
                    .OrderByDescending(participant => participant.Score)
                    .ThenByDescending(participant => participant.AvatarAddr == currentAvatarAddr)
                    .ToList();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
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
                Debug.Log($"Failed to get {nameof(ArenaParticipantModel)}");

                // TODO!!!! [`_playersArenaParticipant`]를 이 문맥이 아닌 곳에서
                // 따로 처리합니다.
                _playerArenaInfo.SetValueAndForceNotify(playerArenaInf);
                return avatarAddrAndScoresWithRank;
            }

            var playerArenaInfo = arenaInfo.FirstOrDefault(p => p.AvatarAddr == currentAvatarAddr);
            if (playerArenaInfo is null)
            {
                playerArenaInf.Rank = arenaInfo.Max(r => r.Rank);
                playerArenaInfo = playerArenaInf;
                arenaInfo.Add(playerArenaInfo);
            }
            else
            {
                playerArenaInfo.Cp = cp;
                playerArenaInfo.PortraitId = portraitId;
            }
            _playerArenaInfo.SetValueAndForceNotify(playerArenaInfo);
            // NOTE: If the [`addrBulk`] is too large, and split and get separately.
            _purchasedDuringInterval.SetValueAndForceNotify(purchasedCountDuringInterval);
            _lastBattleBlockIndex.SetValueAndForceNotify(lastBattleBlockIndex);

            return arenaInfo;
        }
    }
}
