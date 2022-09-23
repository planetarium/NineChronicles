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
        private static readonly Dictionary<Address, WorldBossKillRewardRecord> _killRewards = new();
        private static readonly List<IDisposable> _disposables = new();


        private static ReactiveDictionary<Address, bool> _hasSeasonRewards { get; } = new();
        private static ReactiveDictionary<Address, bool> _canReceiveSeasonRewards { get; } = new();
        private static ReactiveDictionary<Address, bool> _receivingSeasonRewards { get; } = new();
        private static ReactiveDictionary<Address, bool> _hasGradeRewards { get; } = new();
        private static ReactiveDictionary<Address, bool> _receivingGradeRewards { get; } = new();


        public static bool HasSeasonRewards(Address avatarAddress)
        {
            return _hasSeasonRewards.ContainsKey(avatarAddress) && _hasSeasonRewards[avatarAddress];
        }

        public static void SetHasSeasonRewards(Address avatarAddress, bool value)
        {
            if (_hasSeasonRewards.ContainsKey(avatarAddress))
            {
                _hasSeasonRewards.Remove(avatarAddress);
            }

            _hasSeasonRewards.Add(avatarAddress, value);
        }

        public static bool CanReceiveSeasonRewards(Address avatarAddress)
        {
            return _canReceiveSeasonRewards.ContainsKey(avatarAddress) && _canReceiveSeasonRewards[avatarAddress];
        }

        private static void SetCanReceiveSeasonRewards(Address avatarAddress, bool value)
        {
            if (_canReceiveSeasonRewards.ContainsKey(avatarAddress))
            {
                _canReceiveSeasonRewards.Remove(avatarAddress);
            }

            _canReceiveSeasonRewards.Add(avatarAddress, value);
        }

        public static bool IsReceivingSeasonRewards(Address avatarAddress)
        {
            return _receivingSeasonRewards.ContainsKey(avatarAddress) && _receivingSeasonRewards[avatarAddress];
        }

        public static void SetReceivingSeasonRewards(Address avatarAddress, bool value)
        {
            if (_receivingSeasonRewards.ContainsKey(avatarAddress))
            {
                _receivingSeasonRewards.Remove(avatarAddress);
            }

            _receivingSeasonRewards.Add(avatarAddress, value);
        }

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
                SetCanReceiveSeasonRewards(avatarAddress, false);
            }
            else
            {
                SetCanReceiveSeasonRewards(avatarAddress, CanReceivedSeasonReward(raidRow, raider));
                ClearRaiderState();
            }

            SetHasGradeRewards(avatarAddress, IsExistGradeReward(raidRow, raider));
        }

        public static void SubscribeHasSeasonRewards(System.Action callback)
        {
            _hasSeasonRewards.ObserveAdd().Subscribe(x =>
            {
                callback?.Invoke();
            }).AddTo(_disposables);
        }

        public static void SubscribeCanReceivedSeasonRewards(System.Action callback)
        {
            _canReceiveSeasonRewards.ObserveAdd().Subscribe(x =>
            {
                callback?.Invoke();
            }).AddTo(_disposables);
        }

        public static void SubscribeReceivingSeasonRewards(System.Action callback)
        {
            _receivingSeasonRewards.ObserveAdd().Subscribe(x =>
            {
                callback?.Invoke();
            }).AddTo(_disposables);
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

            Widget.Find<WorldBossRewardScreen>().CachingInformation(raiderState, row.BossId);
            var latestRewardRank = raiderState?.LatestRewardRank ?? 0;
            var highScore = raiderState?.HighScore ?? 0;
            var currentRank = WorldBossHelper.CalculateRank(characterRow, highScore);
            return latestRewardRank < currentRank;
        }

        private static bool CanReceivedSeasonReward(WorldBossListSheet.Row row, RaiderState raiderState)
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
