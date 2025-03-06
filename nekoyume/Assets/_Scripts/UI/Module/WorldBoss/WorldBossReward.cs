using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Libplanet.Crypto;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;


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

        [SerializeField]
        private List<GameObject> notificationsSeasonReward;

        [SerializeField]
        private List<GameObject> notificationsBattleGrade;

        [SerializeField]
        private GameObject emptyContainer;

        [SerializeField]
        private TextMeshProUGUI title;

        private readonly ReactiveProperty<ToggleType> _selectedItemSubType = new();
        private Address _cachedAvatarAddress;

        private WorldBossSeasonReward SeasonReward => categoryToggles.First(t => t.Type == ToggleType.SeasonRanking).Item as WorldBossSeasonReward;

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
                foreach (var notification in notificationsBattleGrade)
                {
                    notification.SetActive(b);
                }
            });

            WorldBossStates.SubscribeWorldBossState(state =>
            {
                var currentState = Game.Game.instance.States.CurrentAvatarState;
                if (currentState is null)
                {
                    foreach (var notification in notificationsSeasonReward)
                    {
                        notification.SetActive(false);
                    }
                    return;
                }

                var avatarAddress = currentState.address;
                var isOnSeason = WorldBossStates.IsOnSeason;
                var preRaiderState = WorldBossStates.GetPreRaiderState(avatarAddress);
                foreach (var notification in notificationsSeasonReward)
                {
                    if (preRaiderState is null)
                    {
                        notification.SetActive(false);
                        return;
                    }

                    notification.SetActive(!isOnSeason && !preRaiderState.HasClaimedReward);
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

            var (worldBoss, raider, raidId, isOnSeason) = await WorldBossStates.Set();
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

        public void OnRenderSeasonReward()
        {
            if (SeasonReward == null)
            {
                return;
            }

            SeasonReward.OnRender();
        }
    }
}
