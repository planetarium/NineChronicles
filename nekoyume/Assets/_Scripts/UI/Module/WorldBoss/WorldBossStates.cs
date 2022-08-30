using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;

namespace Nekoyume.UI.Module.WorldBoss
{
    using UniRx;
    public static class WorldBossStates
    {
        private static readonly Dictionary<Address, RaiderState> _raiderStates = new();
        private static readonly List<IDisposable> _disposables = new();

        public static ReactiveProperty<bool> HasNotification { get; } = new();

        public static RaiderState GetRaiderState(Address avatarAddress)
        {
          return _raiderStates.ContainsKey(avatarAddress)
              ? _raiderStates[avatarAddress]
              : null;
        }

        public static void ClearRaiderState()
        {
            _raiderStates.Clear();
        }

        public static async Task Set(Address avatarAddress)
        {
            var (raidId, raider, isOnSeason) = await GetDataAsync();
            if (isOnSeason)
            {
                UpdateRaiderState(avatarAddress, raider);
            }

            var hasNotification = IsExistReward(raidId, raider);
            HasNotification.SetValueAndForceNotify(hasNotification);
        }

        public static void SubscribeNotification(Action<bool> callback)
        {
            HasNotification.Subscribe(callback).AddTo(_disposables);
        }

        public static void UpdateRaiderState(Address avatarAddress, RaiderState raiderState)
        {
            if (_raiderStates.ContainsKey(avatarAddress))
            {
                _raiderStates[avatarAddress] = raiderState;
            }
            else
            {
                _raiderStates.Add(avatarAddress, raiderState);
            }
        }

        private static bool IsExistReward(int raidId, RaiderState raiderState)
        {
            if (!WorldBossFrontHelper.TryGetRaid(raidId, out var row))
            {
                return false;
            }

            var rewardSheet = Game.Game.instance.TableSheets.WorldBossRankRewardSheet;
            var rows = rewardSheet.Values.Where(x => x.BossId.Equals(row.BossId)).ToList();
            if (!rows.Any())
            {
                return false;
            }

            Widget.Find<WorldBossRewardPopup>().CachingInformation(raiderState, row.BossId);
            var latestRewardRank = raiderState?.LatestRewardRank ?? 0;
            var highScore = raiderState?.HighScore ?? 0;
            var currentRank = WorldBossHelper.CalculateRank(highScore);
            return latestRewardRank < currentRank;
        }

        private static async Task<(int raidId, RaiderState raider, bool isOnSeason)> GetDataAsync()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var bossSheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;

            var task = Task.Run(async () =>
            {
                int raidId;
                bool isOnSeason = false;
                try // If the current raidId cannot be found, it will look for the previous raidId.
                {
                    raidId = bossSheet.FindRaidIdByBlockIndex(blockIndex);
                }
                catch (InvalidOperationException)
                {
                    raidId = bossSheet.FindPreviousRaidIdByBlockIndex(blockIndex);
                }

                var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, raidId);
                var raiderState = await Game.Game.instance.Agent.GetStateAsync(raiderAddress);
                var raider = raiderState is Bencodex.Types.List raiderList
                    ? new RaiderState(raiderList)
                    : null;

                return (raidId, raider, isOnSeason);
            });

            await task;
            return task.Result;
        }
    }
}
