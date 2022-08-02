using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nekoyume.Extensions;
using Nekoyume.Game.Controller;
using Nekoyume.Model.State;
using Nekoyume.State;
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
        private RaiderState _cachedRaiderState;

        protected void Awake()
        {
            foreach (var categoryToggle in categoryToggles)
            {
                categoryToggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    _selectedItemSubType.Value = categoryToggle.Type;
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

        public async void ShowAsync(bool isReset = true)
        {
            if (States.Instance.CurrentAvatarState is null)
            {
                return;
            }

            var (raider, raidId) = await GetStatesAsync();
            foreach (var toggle in categoryToggles)
            {
                switch (toggle.Item)
                {
                    case WorldBossSeasonReward season:
                        break;
                    case WorldBossBattleReward battle:
                        break;
                    case WorldBossGradeReward grade:
                        grade.Set(raider, raidId);
                        break;
                }
            }

            if (isReset)
            {
                foreach (var toggle in categoryToggles)
                {
                    toggle.Item.gameObject.SetActive(false);
                }

                categoryToggles.First().Toggle.isOn = true;
                _selectedItemSubType.SetValueAndForceNotify(ToggleType.SeasonRanking);
            }
        }

        private async Task<(RaiderState, int)> GetStatesAsync()
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

                return (raider, raidId);
            });

            await task;
            return task.Result;
        }
    }
}
