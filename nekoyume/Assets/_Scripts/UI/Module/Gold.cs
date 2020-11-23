using System;
using Libplanet.Assets;
using Nekoyume.State;
using Nekoyume.UI.Module.Common;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
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
            _disposable = ReactiveAgentState.Gold.Subscribe(SetGold);
        }

        protected override void OnDisable()
        {
            _disposable.Dispose();
            base.OnDisable();
        }

        private void SetGold(FungibleAssetValue gold)
        {
            text.text = gold.GetQuantityString();
        }

        private void OnClickOnlineShopButton()
        {
            Application.OpenURL(OnlineShopLink);
        }
    }
}
