using System;
using System.Text;
using Libplanet.Common;
using Nekoyume.State;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EventView : MonoBehaviour
    {
        [SerializeField]
        private Image eventImage;

        [SerializeField]
        private Button eventDetailButton;

        private string _url;
        private bool _useAgentAddress;

        private void Awake()
        {
            eventDetailButton.onClick.AddListener(() =>
            {
                var url = _url;
                if (_useAgentAddress)
                {
                    var address = States.Instance.AgentState.address;
                    url = string.Format(url, address);
                }

                Application.OpenURL(url);
            });
        }

        public void Set(Sprite eventSprite, string url, bool useAgentAddress)
        {
            var urlRoot = url;
            var agentAddress = Game.Game.instance.Agent.Address.ToString();
            var message = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var privateKey = Game.Game.instance.Agent.PrivateKey;
            var publicKey = privateKey.PublicKey;

            var signData = Encoding.UTF8.GetBytes($"{message}");
            var hexSignData = ByteUtil.Hex(signData);
            var hash = Helper.Util.ComputeHash(hexSignData);
            var singed = privateKey.Sign(ByteUtil.ParseHex(hash));

            var signature = ByteUtil.Hex(singed);
            url = $"{urlRoot}?app=ninechronicles&agentAddress={agentAddress}&timestamp={message}&signature={signature}&pubkey={publicKey}";

            eventImage.overrideSprite = eventSprite;
            _url = url;
            _useAgentAddress = useAgentAddress;
        }
    }
}
