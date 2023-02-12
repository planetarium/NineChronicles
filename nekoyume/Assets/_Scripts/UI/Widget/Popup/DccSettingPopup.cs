using System.Text;
using Libplanet;
using Libplanet.Crypto;
using Secp256k1Net;
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

        protected override void Awake()
        {
            base.Awake();
            dccButton.onClick.AddListener(ConnectDcc);
            openSeaButton.onClick.AddListener(ConnectOpenSea);
            closeButton.onClick.AddListener(() =>
            {
                Close();
            });

            CloseWidget = () => Close();
        }

        private void ConnectOpenSea()
        {
            Application.OpenURL(OpenSeaURL);
        }

        private void ConnectDcc()
        {
              var agentAddress = "50B8AA5Fd180Cf658Eb1d32926473715c1188476";
                var prefix = "DCC CONNECT";
                var message = "1675664730900";
                var signData = Encoding.UTF8.GetBytes($"{prefix}{message}");

                var hexSignData = ByteUtil.Hex(signData);
                var hash = Helper.Util.ComputeHash(hexSignData);

                var privateKey = new PrivateKey("5435417bf8372f9f08f83f661fd66f8ec25123ba530dcc80de6b5a1b326906dd");
                var singed = privateKey.Sign(ByteUtil.ParseHex(hash));
                var signature = ByteUtil.Hex(singed);

                var secp256K1Signature = new byte[64];
                var ss = new Secp256k1();
                ss.SignatureParseDer(secp256K1Signature, singed);

                var tt = ByteUtil.Hex(secp256K1Signature);
                // ---------------
                // var hashed = HashDigest<SHA256>.DeriveFrom(ByteUtil.ParseHex(hash));
                // var test1 = CryptoConfig.CryptoBackend.Sign(hashed, privateKey);
                // var signature = ByteUtil.Hex(test1);
                // --------------

                // var agentAddress = Game.instance.Agent.Address.ToHex();
                // var prefix = "DCC CONNECT";
                // var message = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                // var signData = Encoding.UTF8.GetBytes($"{prefix}{message}");
                //
                // var hexSignData = ByteUtil.Hex(signData);
                // var hash = Helper.Util.ComputeHash(hexSignData);
                // var privateKey = Game.instance.Agent.PrivateKey;
                //
                // var singed = privateKey.Sign(ByteUtil.ParseHex(hash));
                // var signature = ByteUtil.Hex(singed);


                Debug.Log($"[hash] : {hash}");
                Debug.Log($"[agentAddress] : {agentAddress}");
                Debug.Log($"[data] : {signature}");
                Debug.Log($"[message] : {message}");
                var url =
                $"https://dcc-frontend-git-feat-connect-kimwz.vercel.app/connect?agentAddress=0x{agentAddress}&agentSignTimestamp={message}&agentSignature=0x{signature}";
                Application.OpenURL(url);
        }
    }
}
