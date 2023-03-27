using System;
using System.Text;
using Libplanet;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class DccSettingPopup : PopupWidget
    {
        [SerializeField]
        private Button dccButton;

        [SerializeField]
        private Button openSeaButton;

        [SerializeField]
        private Button closeButton;

        private const string OpenSeaURL = "https://opensea.io/collection/dcc-ninechronicles";
        private const string Prefix = "DCC CONNECT";

        protected override void Awake()
        {
            base.Awake();
            dccButton.onClick.AddListener(ConnectDcc);
            openSeaButton.onClick.AddListener(ConnectOpenSea);
            closeButton.onClick.AddListener(() => { Close(); });

            CloseWidget = () => Close();
        }

        private void ConnectOpenSea()
        {
            Application.OpenURL(OpenSeaURL);
        }

        public void ConnectDcc()
        {
            var agentAddress = Game.Game.instance.Agent.Address.ToHex();
            var message = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var signData = Encoding.UTF8.GetBytes($"{Prefix}{message}");
            var hexSignData = ByteUtil.Hex(signData);
            var hash = Helper.Util.ComputeHash(hexSignData);
            var privateKey = Game.Game.instance.Agent.PrivateKey;
            var publicKey = Game.Game.instance.Agent.PrivateKey.PublicKey;
            var singed = privateKey.Sign(ByteUtil.ParseHex(hash));
            var signature = ByteUtil.Hex(singed);
            var url =
                $"{Game.Game.instance.URL.DccConnect}?agentAddress=0x{agentAddress}&agentSignTimestamp={message}&agentSignature={signature}&agentPub={publicKey}";
            Application.OpenURL(url);
        }
    }
}
