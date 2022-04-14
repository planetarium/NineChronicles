using System;
using Libplanet.Assets;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class Crystal : AlphaAnimateModule
    {
        [SerializeField]
        private TextMeshProUGUI text = null;

        [SerializeField]
        private RectTransform tooltipArea = null;

        private IDisposable _disposable;

        [SerializeField]
        private GameObject loadingObject;

        public bool NowCharging => loadingObject.activeSelf;

        protected override void OnEnable()
        {
            base.OnEnable();
            _disposable = AgentStateSubject.Gold.Subscribe(SetCrystal);
            UpdateCrystal();
        }

        protected override void OnDisable()
        {
            _disposable.Dispose();
            base.OnDisable();
        }

        private void UpdateCrystal()
        {
            if (States.Instance is null ||
                States.Instance.GoldBalanceState is null)
            {
                return;
            }

            SetCrystal(States.Instance.GoldBalanceState.Gold);
        }

        private void SetCrystal(FungibleAssetValue gold)
        {
            text.text = gold.GetQuantityString();
        }

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
