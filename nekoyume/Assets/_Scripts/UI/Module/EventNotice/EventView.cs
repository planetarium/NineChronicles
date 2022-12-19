using System;
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
            eventImage.overrideSprite = eventSprite;
            _url = url;
            _useAgentAddress = useAgentAddress;
        }
    }
}
