using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume.UI.Module.WorldBoss
{
    using Nekoyume.TableData;
    using UniRx;
    public static class WorldBossStates
    {
        private static readonly Dictionary<Address, RaiderState> _raiderStates = new();
        private static readonly Dictionary<Address, WorldBossKillRewardRecord> _killRewards = new();
        private static readonly List<IDisposable> _disposables = new();

        public static ReactiveProperty<bool> HasKillRewards { get; } = new();
        public static ReactiveProperty<bool> HasSeasonRewards { get; } = new();

        public static RaiderState GetRaiderState(Address avatarAddress)
        {
          return _raiderStates.ContainsKey(avatarAddress)
              ? _raiderStates[avatarAddress]
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

        public static async Task Set(Address avatarAddress)
        {
            var (raidRow, raider, killReward, isOnSeason) = await GetDataAsync();
            if (isOnSeason)
            {
                UpdateState(avatarAddress, raider, killReward);
            }
            else
            {
                HasSeasonRewards.SetValueAndForceNotify(IsExistSeasonReward(raidRow, raider));
                ClearRaiderState();
            }

            HasKillRewards.SetValueAndForceNotify(IsExistGradeReward(raidRow, raider));
        }

        public static void SubscribeKillRewards(Action<bool> callback)
        {
            HasKillRewards.Subscribe(callback).AddTo(_disposables);
        }

        public static void SubscribeSeasonRewards(Action<bool> callback)
        {
            HasSeasonRewards.Subscribe(callback).AddTo(_disposables);
        }

        public static void UpdateState(
            Address avatarAddress,
            RaiderState raiderState,
            WorldBossKillRewardRecord killReward)
        {
            if (_raiderStates.ContainsKey(avatarAddress))
            {
                _raiderStates[avatarAddress] = raiderState;
            }
            else
            {
                _raiderStates.Add(avatarAddress, raiderState);
            }

            if (_killRewards.ContainsKey(avatarAddress))
            {
                _killRewards[avatarAddress] = killReward;
            }
            else
            {
                _killRewards.Add(avatarAddress, killReward);
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

            Widget.Find<WorldBossRewardPopup>().CachingInformation(raiderState, row.BossId);
            var latestRewardRank = raiderState?.LatestRewardRank ?? 0;
            var highScore = raiderState?.HighScore ?? 0;
            var currentRank = WorldBossHelper.CalculateRank(characterRow, highScore);
            return latestRewardRank < currentRank;
        }

        private static bool IsExistSeasonReward(WorldBossListSheet.Row row, RaiderState raiderState)
        {
            if (row == null)
            {
                return false;
            }

            return raiderState != null;
        }

        private static async Task<(
            WorldBossListSheet.Row raidRow,
            RaiderState raider,
            WorldBossKillRewardRecord killReward,
            bool isOnSeason)> GetDataAsync()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var bossSheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;

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
                var raiderState = await Game.Game.instance.Agent.GetStateAsync(raiderAddress);
                var raider = raiderState is Bencodex.Types.List raiderList
                    ? new RaiderState(raiderList)
                    : null;

                var killRewardAddress = Addresses.GetWorldBossKillRewardRecordAddress(avatarAddress, raidRow.Id);
                var killRewardState = await Game.Game.instance.Agent.GetStateAsync(killRewardAddress);
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
