using Nekoyume.State;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EventBannerItem : MonoBehaviour
    {
        [SerializeField]
        private string url;

        [SerializeField]
        private bool useAgentAddress;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                var u = url;
                if (useAgentAddress)
                {
                    var address = States.Instance.AgentState.address;
                    u = string.Format(url, address);
                }
                Application.OpenURL(u);
            });
        }

        public void Set(Texture texture, string url)
        {
            GetComponent<RawImage>().texture = texture;
            this.url = url;
        }
    }
}
