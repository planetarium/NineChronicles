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
        private Image _iconImage;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public Image IconImage => _iconImage;

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
            var count = Util.GetHourglassCount(inventory, Game.Game.instance.Agent.BlockIndex);
            countText.text = count.ToString("N0", CultureInfo.CurrentCulture);
        }

        // Call at Event Trigger Component
        public void ShowTooltip()
        {
            Widget.Find<VanilaTooltip>()
                .Show("ITEM_NAME_400000", "UI_HOURGLASS_DESCRIPTION", tooltipArea.position);
        }

        public void HideTooltip()
        {
            Widget.Find<VanilaTooltip>().Close();
        }
    }
}
