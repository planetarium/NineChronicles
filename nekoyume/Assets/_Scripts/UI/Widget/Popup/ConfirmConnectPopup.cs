using System;
using System.Text;
using Libplanet.Common;
using Nekoyume.ApiClient;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ConfirmConnectPopup : PopupWidget
    {
        [Serializable]
        public class ConnectData
        {
            public GameObject root;
            public Button connectButton;
        }

        [SerializeField]
        private ConnectData dccHomepage;

        [SerializeField]
        private ConnectData openSea;

        [SerializeField]
        private Button closeButton;

        private bool _isMileageShop;
        private const string OpenSeaURL = "https://opensea.io/collection/dcc-ninechronicles";
        private const string Prefix = "DCC CONNECT";

        protected override void Awake()
        {
            base.Awake();

            openSea.connectButton.onClick.AddListener(ConnectOpenSea);
            dccHomepage.connectButton.onClick.AddListener(ConnectDcc);

            closeButton.onClick.AddListener(() => { Close(); });
            CloseWidget = () => Close();
        }

        public void ShowConnectDcc(bool isMileageShop = false)
        {
            _isMileageShop = isMileageShop;
            Show();

            dccHomepage.root.SetActive(true);
            openSea.root.SetActive(false);
        }

        public void ShowConnectOpenSea()
        {
            Show();
            dccHomepage.root.SetActive(false);
            openSea.root.SetActive(true);
        }

        private void ConnectDcc()
        {
            var dccUrl = ApiClients.Instance.DccURL;

            string url;
            if (_isMileageShop)
            {
                url = dccUrl.DccMileageShop;
            }
            else
            {
                var urlRoot = dccUrl.DccConnect;
                var agentAddress = Game.Game.instance.Agent.Address.ToHex();
                var message = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                var privateKey = Game.Game.instance.Agent.PrivateKey;
                var publicKey = privateKey.PublicKey;

                var signData = Encoding.UTF8.GetBytes($"{Prefix}{message}");
                var hexSignData = ByteUtil.Hex(signData);
                var hash = Helper.Util.ComputeHash(hexSignData);
                var singed = privateKey.Sign(ByteUtil.ParseHex(hash));

                var signature = ByteUtil.Hex(singed);
                url = $"{urlRoot}?agentAddress=0x{agentAddress}&agentSignTimestamp={message}&agentSignature={signature}&agentPub={publicKey}";
            }

            Application.OpenURL(url);
        }

        private static void ConnectOpenSea()
        {
            Application.OpenURL(OpenSeaURL);
        }
    }
}
