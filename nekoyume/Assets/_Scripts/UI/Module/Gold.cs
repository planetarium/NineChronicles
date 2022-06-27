using System;
using System.Globalization;
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

    public class Gold : AlphaAnimateModule
    {
        [SerializeField]
        private TextMeshProUGUI text = null;

        [SerializeField]
        private Button onlineShopButton = null;

        private IDisposable _disposable;

        private const string OnlineShopLink = "https://shop.nine-chronicles.com/";

        protected void Awake()
        {
            onlineShopButton.onClick.AddListener(OnClickOnlineShopButton);
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
            var str = gold.GetQuantityString();
            text.text = decimal.TryParse(str, out var num)
                ? num.ToString("N0", CultureInfo.CurrentCulture)
                : str;
        }

        private void OnClickOnlineShopButton()
        {
            Application.OpenURL(OnlineShopLink);
        }
    }
}
