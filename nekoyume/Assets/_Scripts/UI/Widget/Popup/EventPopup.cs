using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class EventPopup : PopupWidget
    {
        [SerializeField]
        private Image eventImage;

        [SerializeField]
        private Button eventDetailButton;

        private string _url;
        private bool _useAgentAddress;

        protected override void Awake()
        {
            base.Awake();
            eventDetailButton.onClick.AddListener(() =>
            {
                var confirm = Find<TitleOneButtonSystem>();
                confirm.SubmitCallback = () =>
                {
                    var url = _url;
                    if (_useAgentAddress)
                    {
                        var address = States.Instance.AgentState.address;
                        url = string.Format(url, address);
                    }

                    Application.OpenURL(url);
                    confirm.Close();
                };
                confirm.Set("UI_TITLE_UPGRADE_EVENT", "UI_DESCRIPTION_UPGRADE_EVENT", true);
                confirm.Show();
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
