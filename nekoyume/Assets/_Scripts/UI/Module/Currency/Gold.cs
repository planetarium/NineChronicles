using System;
using Libplanet.Types.Assets;
using Nekoyume.State;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class Gold : AlphaAnimateModule
    {
        [SerializeField]
        private TextMeshProUGUI text = null;

        [SerializeField]
        private Button onlineShopButton = null;

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private Button button;

        private IDisposable _disposable;

        private const string OnlineShopLink = "https://shop.nine-chronicles.com/";

        public Image IconImage => iconImage;

        protected void Awake()
        {
            onlineShopButton.onClick.AddListener(OnClickOnlineShopButton);
            button.onClick.AddListener(ShowMaterialNavigationPopup);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _disposable = AgentStateSubject.Gold.Subscribe(SetGold);
            UpdateGold();
        }

        protected override void OnDisable()
        {
            _disposable.Dispose();
            base.OnDisable();
        }

        private void UpdateGold()
        {
            if (States.Instance is null ||
                States.Instance.GoldBalanceState is null)
            {
                return;
            }

            SetGold(States.Instance.GoldBalanceState.Gold);
        }

        private void SetGold(FungibleAssetValue gold)
        {
            text.text = gold.GetQuantityString();
        }

        private void OnClickOnlineShopButton()
        {
            Helper.Util.OpenURL(OnlineShopLink);
        }

        private void ShowMaterialNavigationPopup()
        {
            Widget.Find<MaterialNavigationPopup>().ShowCurrency(CostType.NCG);
        }
    }
}
