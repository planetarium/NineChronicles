using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Controller;
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
            Reward
        }

        [Serializable]
        private struct CategoryToggle
        {
            public Toggle Toggle;
            public ToggleType Type;
            public WorldBossDetailItem Item;
        }

        [SerializeField]
        private List<CategoryToggle> categoryToggles = null;

        [SerializeField]
        private Button backButton;

        private readonly ReactiveProperty<ToggleType> _selectedItemSubType = new();

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
                    _selectedItemSubType.Value = categoryToggle.Type;
                });
            }

            _selectedItemSubType.Subscribe(UpdateView).AddTo(gameObject);
        }

        public void Show(ToggleType toggleType)
        {
            base.Show();
            categoryToggles.FirstOrDefault(x => x.Type.Equals(toggleType)).Toggle.isOn = true;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            var header = Find<HeaderMenuStatic>();
            header.Show(HeaderMenuStatic.AssetVisibleState.WorldBoss);
            base.Close(ignoreCloseAnimation);
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
