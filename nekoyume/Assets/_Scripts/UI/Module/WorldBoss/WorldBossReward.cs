using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Extensions;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using TMPro;
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
            BattleGrade
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

        [SerializeField]
        private List<GameObject> notifications;

        [SerializeField]
        private GameObject emptyContainer;

        [SerializeField]
        private TextMeshProUGUI title;

        private readonly ReactiveProperty<ToggleType> _selectedItemSubType = new();
        private Address _cachedAvatarAddress;

        protected void Awake()
        {
            foreach (var categoryToggle in categoryToggles)
            {
                categoryToggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (!value)
                    {
                        return;
                    }

                    AudioController.PlayClick();
                    _selectedItemSubType.SetValueAndForceNotify(categoryToggle.Type);
                });
            }

            _selectedItemSubType.Subscribe(toggleType =>
            {
                if (emptyContainer.activeSelf)
                {
                    return;
                }

                foreach (var toggle in categoryToggles)
                {
                    toggle.Item.gameObject.SetActive(toggle.Type.Equals(toggleType));
                }
            }).AddTo(gameObject);

            WorldBossStates.SubscribeGradeRewards((b) =>
            {
                foreach (var notification in notifications)
                {
                    notification.SetActive(b);
                }
            });
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

            var (worldBoss, raider, raidId, isOnSeason) = await GetDataAsync();
            CheckRaidId(raidId);
            UpdateTitle();

            foreach (var toggle in categoryToggles)
            {
                switch (toggle.Item)
                {
                    case WorldBossSeasonReward season:
                        season.Set(worldBoss, raider, raidId, isOnSeason);
                        break;
                    case WorldBossBattleReward battle:
                        battle.Set(raidId);
                        break;
                    case WorldBossGradeReward grade:
                        grade.Set(raider, raidId);
                        break;
                }
            }
        }

        private void CheckRaidId(int raidId)
        {
            if (WorldBossFrontHelper.TryGetRaid(raidId, out _))
            {
                emptyContainer.SetActive(false);
            }
            else
            {
                emptyContainer.SetActive(true);
                foreach (var toggle in categoryToggles)
                {
                    toggle.Item.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateTitle()
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var status = WorldBossFrontHelper.GetStatus(blockIndex);
            title.text = status == WorldBossStatus.Season
                ? L10nManager.Localize("UI_REWARDS")
                : $"{L10nManager.Localize("UI_PREVIOUS")} {L10nManager.Localize("UI_REWARDS")}";
        }

        private async Task<(WorldBossState worldBoss, RaiderState raiderState, int raidId, bool isOnSeason)> GetDataAsync()
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
                        return (null, null, 0, false);
                    }
                }
                var raidId = raidRow.Id;

                var worldBossAddress = Addresses.GetWorldBossAddress(raidId);
                var worldBossState = await Game.Game.instance.Agent.GetStateAsync(
                    ReservedAddresses.LegacyAccount,
                    worldBossAddress);
                var worldBoss = worldBossState is Bencodex.Types.List worldBossList
                    ? new WorldBossState(worldBossList)
                    : null;

                var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, raidId);
                var raiderState = await Game.Game.instance.Agent.GetStateAsync(
                    ReservedAddresses.LegacyAccount,
                    raiderAddress);
                var raider = raiderState is Bencodex.Types.List raiderList
                    ? new RaiderState(raiderList)
                    : null;
                return (worldBoss, raider, raidId, isOnSeason);
            });

            await task;
            return task.Result;
        }
    }
}
