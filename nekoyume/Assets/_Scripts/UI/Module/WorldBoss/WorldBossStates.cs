using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Common;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;

namespace Nekoyume.UI.Module.WorldBoss
{
    using TableData;
    using UniRx;

    public static class WorldBossStates
    {
        private static readonly Dictionary<Address, RaiderState> _raiderStates = new();
        private static readonly Dictionary<Address, RaiderState> _preRaiderStates = new();
        private static readonly Dictionary<Address, WorldBossKillRewardRecord> _killRewards = new();
        private static readonly List<IDisposable> _disposables = new();

        private static ReactiveDictionary<Address, bool> _hasGradeRewards { get; } = new();
        private static ReactiveDictionary<Address, bool> _receivingGradeRewards { get; } = new();
        
        private static long _lastUpdatedStateBlockIndex = -1;

        public static void SetHasGradeRewards(Address avatarAddress, bool value)
        {
            if (_hasGradeRewards.ContainsKey(avatarAddress))
            {
                _hasGradeRewards.Remove(avatarAddress);
            }

            _hasGradeRewards.Add(avatarAddress, value);
        }

        public static bool IsReceivingGradeRewards(Address avatarAddress)
        {
            if (!_receivingGradeRewards.ContainsKey(avatarAddress))
            {
                _receivingGradeRewards.Add(avatarAddress, false);
            }

            return _receivingGradeRewards[avatarAddress];
        }

        public static void SetReceivingGradeRewards(Address avatarAddress, bool value)
        {
            if (_receivingGradeRewards.ContainsKey(avatarAddress))
            {
                _receivingGradeRewards.Remove(avatarAddress);
            }

            _receivingGradeRewards.Add(avatarAddress, value);
        }


        public static RaiderState GetRaiderState(Address avatarAddress)
        {
            return _raiderStates.ContainsKey(avatarAddress)
                ? _raiderStates[avatarAddress]
                : null;
        }

        public static RaiderState GetPreRaiderState(Address avatarAddress)
        {
            return _preRaiderStates.ContainsKey(avatarAddress)
                ? _preRaiderStates[avatarAddress]
                : null;
        }

        public static WorldBossKillRewardRecord GetKillReward(Address avatarAddress)
        {
            return _killRewards.ContainsKey(avatarAddress)
                ? _killRewards[avatarAddress]
                : null;
        }

        public static void ClearRaiderState()
        {
            _raiderStates.Clear();
            _killRewards.Clear();
        }

        public static async UniTask Set(
            HashDigest<SHA256> hash,
            long blockIndex,
            Address avatarAddress)
        {
            var (raidRow, raider, killReward, isOnSeason) =
                await GetDataAsync(hash, blockIndex, avatarAddress);
            if (isOnSeason)
            {
                UpdateState(avatarAddress, raider, killReward, true);
            }
            else
            {
                UpdatePreRaiderState(avatarAddress, raider);
                ClearRaiderState();
            }

            SetHasGradeRewards(avatarAddress, IsExistGradeReward(raidRow, raider));
        }

        public static void SubscribeGradeRewards(Action<bool> callback)
        {
            _hasGradeRewards.ObserveAdd().Subscribe(x =>
            {
                var address = States.Instance.CurrentAvatarState.address;
                callback?.Invoke(address == x.Key && x.Value);
            }).AddTo(_disposables);
        }

        public static void SubscribeReceivingGradeRewards(Action<bool> callback)
        {
            _receivingGradeRewards.ObserveAdd().Subscribe(x =>
            {
                var address = States.Instance.CurrentAvatarState.address;
                callback?.Invoke(address == x.Key && x.Value);
            }).AddTo(_disposables);
        }

        /// <summary>
        /// 월드보스 관련 상태를 업데이트. sloth 업데이트를 대응하기 위해
        /// action으로 부터 받아오는 경우 값을 항상 갱신하고, 아닌 경우
        /// 마지막으로 갱신된 blockIndex와 비교하여 업데이트한다.
        /// </summary>
        public static void UpdateState(
            Address avatarAddress,
            RaiderState raiderState,
            WorldBossKillRewardRecord killReward,
            bool forceUpdate = false)
        {
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            if (!forceUpdate && _lastUpdatedStateBlockIndex >= currentBlockIndex)
            {
                return;
            }
            
            _lastUpdatedStateBlockIndex = currentBlockIndex;
            
            _raiderStates[avatarAddress] = raiderState;
            _killRewards[avatarAddress] = killReward;
        }

        private static void UpdatePreRaiderState(Address avatarAddress, RaiderState raiderState)
        {
            if (_preRaiderStates.ContainsKey(avatarAddress))
            {
                _preRaiderStates[avatarAddress] = raiderState;
            }
            else
            {
                _preRaiderStates.Add(avatarAddress, raiderState);
            }
        }

        private static bool IsExistGradeReward(WorldBossListSheet.Row row, RaiderState raiderState)
        {
            if (row is null || raiderState is null)
            {
                return false;
            }

            var tableSheets = Game.Game.instance.TableSheets;
            var rewardSheet = tableSheets.WorldBossRankRewardSheet;
            var rows = rewardSheet.Values.Where(x => x.BossId.Equals(row.BossId)).ToList();
            if (!rows.Any())
            {
                return false;
            }

            if (!tableSheets.WorldBossCharacterSheet.TryGetValue(row.BossId, out var characterRow))
            {
                return false;
            }

            Widget.Find<WorldBossRewardScreen>().CachingInformation(raiderState, row.BossId);
            var latestRewardRank = raiderState?.LatestRewardRank ?? 0;
            var highScore = raiderState?.HighScore ?? 0;
            var currentRank = WorldBossHelper.CalculateRank(characterRow, highScore);
            return latestRewardRank < currentRank;
        }

        private static async Task<(
            WorldBossListSheet.Row raidRow,
            RaiderState raider,
            WorldBossKillRewardRecord killReward,
            bool isOnSeason)> GetDataAsync(
            HashDigest<SHA256> hash,
            long blockIndex,
            Address avatarAddress)
        {
            var bossSheet = Game.Game.instance.TableSheets.WorldBossListSheet;

            var task = Task.Run(async () =>
            {
                WorldBossListSheet.Row raidRow;
                var isOnSeason = false;

                try
                {
                    raidRow = bossSheet.FindRowByBlockIndex(blockIndex);
                    isOnSeason = true;
                }
                catch (InvalidOperationException)
                {
                    try
                    {
                        raidRow = bossSheet.FindPreviousRowByBlockIndex(blockIndex);
                    }
                    catch (InvalidOperationException)
                    {
                        return (null, null, null, false);
                    }
                }

                var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, raidRow.Id);
                var raiderState = await Game.Game.instance.Agent.GetStateAsync(
                    hash, ReservedAddresses.LegacyAccount, raiderAddress);
                var raider = raiderState is Bencodex.Types.List raiderList
                    ? new RaiderState(raiderList)
                    : null;

                var killRewardAddress = Addresses.GetWorldBossKillRewardRecordAddress(avatarAddress, raidRow.Id);
                var killRewardState = await Game.Game.instance.Agent.GetStateAsync(
                    hash, ReservedAddresses.LegacyAccount, killRewardAddress);
                var killReward = killRewardState is Bencodex.Types.List killRewardList
                    ? new WorldBossKillRewardRecord(killRewardList)
                    : null;

                return (raidRow, raider, killReward, isOnSeason);
            });

            await task;
            return task.Result;
        }
    }
}
