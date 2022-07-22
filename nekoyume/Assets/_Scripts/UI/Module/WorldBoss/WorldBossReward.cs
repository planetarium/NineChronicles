
using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI.Module.WorldBoss
{
    using UniRx;

    public class WorldBossReward : WorldBossDetailItem
    {
        private enum ToggleType
        {
            SeasonRanking,
            BossBattle,
            BattleRank,
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

            _selectedItemSubType.Subscribe(UpdateView).AddTo(gameObject);
        }

        private void UpdateView(ToggleType toggleType)
        {
            foreach (var toggle in categoryToggles)
            {
                toggle.Item.gameObject.SetActive(toggle.Type.Equals(toggleType));
            }
        }
    }
}
