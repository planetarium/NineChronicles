using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet;
using Nekoyume.Extensions;
using Nekoyume.Game.Controller;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Module.WorldBoss
{
    using UniRx;

    public class WorldBossReward : WorldBossDetailItem
    {
        public enum ToggleType
        {
            SeasonRanking,
            BossBattle,
            BattleGrade,
        }

        [Serializable]
        private struct CategoryToggle
        {
            public Toggle Toggle;
            public ToggleType Type;
            public WorldBossRewardItem Item;
        }

        [SerializeField]
        private List<CategoryToggle> categoryToggles = null;

        private readonly ReactiveProperty<ToggleType> _selectedItemSubType = new();
        private Address _cachedAvatarAddress;

        protected void Awake()
        {
            foreach (var categoryToggle in categoryToggles)
            {
                categoryToggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    _selectedItemSubType.SetValueAndForceNotify(categoryToggle.Type);
                });
            }

            _selectedItemSubType.Subscribe(toggleType =>
            {
                foreach (var toggle in categoryToggles)
                {
                    toggle.Item.gameObject.SetActive(toggle.Type.Equals(toggleType));
                }
            }).AddTo(gameObject);
        }

        private IEnumerator CoSetFirstCategory()
        {
            yield return null;
            categoryToggles.First().Toggle.isOn = true;
            _selectedItemSubType.SetValueAndForceNotify(ToggleType.SeasonRanking);
        }

        public async void ShowAsync(bool isReset = true)
        {
            if (States.Instance.CurrentAvatarState is null)
            {
                return;
            }

            if (_cachedAvatarAddress != States.Instance.CurrentAvatarState.address)
            {
                foreach (var toggle in categoryToggles)
                {
                    toggle.Item.gameObject.SetActive(false);
                    toggle.Item.Reset();
                }

                _cachedAvatarAddress = States.Instance.CurrentAvatarState.address;
            }

            if (isReset)
            {
                foreach (var toggle in categoryToggles)
                {
                    toggle.Item.gameObject.SetActive(false);
                }

                StartCoroutine(CoSetFirstCategory());
            }

            var (raider, killRewardRecord, raidId, record, userCount) = await GetDataAsync();
            foreach (var toggle in categoryToggles)
            {
                switch (toggle.Item)
                {
                    case WorldBossSeasonReward season:
                        var rank = record?.Ranking ?? 0;
                        season.Set(raidId, rank, userCount);
                        break;
                    case WorldBossBattleReward battle:
                        battle.Set(raidId, record);
                        break;
                    case WorldBossGradeReward grade:
                        grade.Set(raider, raidId);
                        break;
                }
            }
        }

        private async Task<(
            RaiderState raider,
            WorldBossKillRewardRecord killReward,
            int raidId,
            WorldBossRankingRecord record,
            int userCount)>
            GetDataAsync()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var bossSheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;

            var task = Task.Run(async () =>
            {
                int raidId;
                try
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

                var killRewardAddress = Addresses.GetWorldBossKillRewardRecordAddress(avatarAddress, raidId);
                var killRewardState = await Game.Game.instance.Agent.GetStateAsync(killRewardAddress);
                var killReward = killRewardState is Bencodex.Types.List killRewardList
                    ? new WorldBossKillRewardRecord(killRewardList)
                    : null;

                var response = await WorldBossQuery.QueryRankingAsync(raidId, avatarAddress);
                var records = response?.WorldBossRanking ?? new List<WorldBossRankingRecord>();
                var userCount = response?.WorldBossTotalUsers ?? 0;
                var myRecord = records.FirstOrDefault(record => record.Address == avatarAddress.ToHex());
                return (raider, killReward, raidId, myRecord, userCount);
            });

            await task;
            return task.Result;
        }
    }
}
