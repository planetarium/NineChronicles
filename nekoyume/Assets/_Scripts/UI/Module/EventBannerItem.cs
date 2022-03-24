using System;
using Nekoyume.State;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EventBannerItem : MonoBehaviour
    {
        [SerializeField, Tooltip("checked: use `beginDateTime` and `endDateTime`\nor not: not use")]
        private bool useDateTime;

        [SerializeField,
         Tooltip("<yyyy-MM-ddTHH:mm:ss> (UTC) Appear this banner item since.(e.g., 2022-03-22T13:00:00")]
        private string beginDateTime;

        [SerializeField,
         Tooltip("<yyyy-MM-ddTHH:mm:ss> (UTC) Disappear this banner item since.(e.g., 2022-03-22T14:00:00")]
        private string endDateTime;

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

        public bool IsInTime()
        {
            if (!useDateTime)
                return true;

            return DateTime.UtcNow.IsInTime(beginDateTime, endDateTime);
        }
    }
}
