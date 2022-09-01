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
    using Nekoyume.TableData;
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
            var (raidRow, raider, isOnSeason) = await GetDataAsync();
            if (isOnSeason)
            {
                UpdateRaiderState(avatarAddress, raider);
            }
            else
            {
                ClearRaiderState();
            }

            var hasNotification = IsExistReward(raidRow, raider);
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

        private static bool IsExistReward(WorldBossListSheet.Row row, RaiderState raiderState)
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

        private static async Task<(WorldBossListSheet.Row raidRow, RaiderState raider, bool isOnSeason)> GetDataAsync()
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
                        return (null, null, false);
                    }
                }

                var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, raidRow.Id);
                var raiderState = await Game.Game.instance.Agent.GetStateAsync(raiderAddress);
                var raider = raiderState is Bencodex.Types.List raiderList
                    ? new RaiderState(raiderList)
                    : null;

                return (raidRow, raider, isOnSeason);
            });

            await task;
            return task.Result;
        }
    }
}
