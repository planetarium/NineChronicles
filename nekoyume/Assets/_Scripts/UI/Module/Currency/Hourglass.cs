using System;
using System.Collections.Generic;
using System.Globalization;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class Hourglass : AlphaAnimateModule
    {
        [SerializeField]
        private TextMeshProUGUI countText = null;

        [SerializeField]
        private RectTransform tooltipArea = null;

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private Button button;

        private readonly List<IDisposable> _disposables = new();

        public Image IconImage => iconImage;

        private void Awake()
        {
            button.onClick.AddListener(ShowMaterialNavigationPopup);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ReactiveAvatarState.Inventory?.Subscribe(UpdateHourglass).AddTo(_disposables);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _disposables.DisposeAllAndClear();
        }

        private void UpdateHourglass(Nekoyume.Model.Item.Inventory inventory)
        {
            var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
            countText.text = inventory
                .GetUsableItemCount(CostType.Hourglass, blockIndex)
                .ToString("N0", CultureInfo.CurrentCulture);
        }

        private void ShowMaterialNavigationPopup()
        {
            Widget.Find<MaterialNavigationPopup>().ShowCurrency(CostType.Hourglass);
        }
    }
}
