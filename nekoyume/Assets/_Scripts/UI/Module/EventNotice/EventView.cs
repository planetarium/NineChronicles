using System;
using System.Text;
using Libplanet.Common;
using Nekoyume.Helper;
using Nekoyume.State;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public enum EventButtonType
    {
        URL,
        IN_GAME,
    }

    [Serializable]
    public class InGameNavigationData
    {
        public Summon.SummonType SummonType { get; set; }
    }

    public class EventView : MonoBehaviour
    {
        [SerializeField]
        private Image eventImage;

        [SerializeField]
        private Button urlButton;

        [SerializeField]
        private CallbackButton navigationButton;

        [SerializeField]
        private EventReleaseNotePopup parent;

        private string _url;
        private bool _useAgentAddress;
        private EventButtonType _buttonType;
        private InGameNavigationData _inGameNavigationData;

        private void Awake()
        {
            urlButton.onClick.AddListener(() =>
            {
                var url = _url;
                if (_useAgentAddress)
                {
                    var address = States.Instance.AgentState.address;
                    url = string.Format(url, address);
                }

                Helper.Util.OpenURL(url);
            });
        }

        public void Set(Sprite eventSprite, string url, bool useAgentAddress, bool sign)
        {
            Set(eventSprite, url, useAgentAddress, sign, EventButtonType.URL);
        }

        public void Set(
            Sprite eventSprite,
            string url,
            bool useAgentAddress,
            bool sign,
            EventButtonType buttonType,
            InGameNavigationData inGameNavigationData = null
        )
        {
            if (sign && buttonType == EventButtonType.URL)
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
                url =
                    $"{urlRoot}?app=ninechronicles&agentAddress={agentAddress}&timestamp={message}&signature={signature}&pubkey={publicKey}";
            }

            eventImage.overrideSprite = eventSprite;
            _url = url;
            _useAgentAddress = useAgentAddress;
            _buttonType = buttonType;
            _inGameNavigationData = inGameNavigationData;

            if (_buttonType == EventButtonType.URL)
            {
                urlButton.gameObject.SetActive(true);
                navigationButton.gameObject.SetActive(false);
            }
            else if (_buttonType == EventButtonType.IN_GAME)
            {
                navigationButton.gameObject.SetActive(true);
                urlButton.gameObject.SetActive(false);

                if (_inGameNavigationData == null)
                    return;

                var action = ShortcutHelper.GetSummonShortcutAction(
                    parent,
                    _inGameNavigationData.SummonType
                );
                navigationButton.Set(action);
            }
        }
    }
}
