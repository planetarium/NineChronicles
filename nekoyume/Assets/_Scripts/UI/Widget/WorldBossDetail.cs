using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.WorldBoss;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    using UniRx;

    public class WorldBossDetail : Widget
    {
        public enum ToggleType
        {
            Information,
            PreviousRank,
            Rank,
            Reward,
            Rune,
        }

        [Serializable]
        private struct CategoryToggle
        {
            public Toggle Toggle;
            public ToggleType Type;
            public WorldBossDetailItem Item;
        }

        [SerializeField]
        private List<CategoryToggle> categoryToggles;

        [SerializeField]
        private Button backButton;

        [SerializeField]
        private GameObject notification;

        private readonly ReactiveProperty<ToggleType> _selectedItemSubType = new(ToggleType.Reward);

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = () => { Close(); };
            backButton.OnClickAsObservable().Subscribe(_ => { Close(); }).AddTo(gameObject);
            foreach (var categoryToggle in categoryToggles)
            {
                categoryToggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (!value) return;
                    AudioController.PlayClick();
                    _selectedItemSubType.SetValueAndForceNotify(categoryToggle.Type);
                });
            }

            _selectedItemSubType.Subscribe(UpdateView).AddTo(gameObject);
            WorldBossStates.SubscribeGradeRewards((b) => notification.SetActive(b));
        }

        public void Show(ToggleType toggleType)
        {
            base.Show();
            var toggle = categoryToggles.FirstOrDefault(x => x.Type.Equals(toggleType));
            toggle.Item.gameObject.SetActive(true);
            toggle.Toggle.isOn = true;
            _selectedItemSubType.SetValueAndForceNotify(toggleType);
        }

        public void GotRewards() // For ClaimRaidReward Action Render
        {
            var reward = categoryToggles.FirstOrDefault(x => x.Type == ToggleType.Reward);
            if (reward.Item is not WorldBossReward worldBossReward)
            {
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            WorldBossStates.SetHasGradeRewards(avatarAddress, false);
            worldBossReward.ShowAsync(false);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            var headerMenu = Find<HeaderMenuStatic>();
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            switch (WorldBossFrontHelper.GetStatus(currentBlockIndex))
            {
                case WorldBossStatus.OffSeason:
                    headerMenu.Show(HeaderMenuStatic.AssetVisibleState.CurrencyOnly);
                    break;
                case WorldBossStatus.Season:
                    headerMenu.Show(HeaderMenuStatic.AssetVisibleState.WorldBoss);
                    break;
                case WorldBossStatus.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
            base.Close(ignoreCloseAnimation);
        }

        private void UpdateView(ToggleType toggleType)
        {
            var rankToggle = categoryToggles.FirstOrDefault(x => x.Type == ToggleType.Rank);
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var itInSeason = WorldBossFrontHelper.IsItInSeason(currentBlockIndex);
            rankToggle.Toggle.gameObject.SetActive(itInSeason);

            foreach (var toggle in categoryToggles.Where(toggle => toggle.Type != toggleType))
            {
                toggle.Item.gameObject.SetActive(false);
            }

            var categoryToggle = categoryToggles.FirstOrDefault(toggle => toggle.Type.Equals(toggleType));
            categoryToggle.Item.gameObject.SetActive(true);

            switch (categoryToggle.Type)
            {
                case ToggleType.Information:
                    if (categoryToggle.Item is WorldBossInformation information)
                    {
                        information.Show();
                    }
                    break;
                case ToggleType.PreviousRank:
                    if (categoryToggle.Item is WorldBossRank previousRank)
                    {
                        previousRank.ShowAsync(WorldBossRank.Status.PreviousSeason);
                    }
                    break;
                case ToggleType.Rank:
                    if (categoryToggle.Item is WorldBossRank rank)
                    {
                        rank.ShowAsync(WorldBossRank.Status.Season);
                    }
                    break;
                case ToggleType.Reward:
                    if (categoryToggle.Item is WorldBossReward reward)
                    {
                        reward.ShowAsync();
                    }
                    break;
                case ToggleType.Rune:
                    if (categoryToggle.Item is WorldBossRuneStoneInventory inventory)
                    {
                        inventory.Show();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
