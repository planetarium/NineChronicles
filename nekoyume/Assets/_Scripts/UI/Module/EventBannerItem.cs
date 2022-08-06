using System;
using System.Collections;
using Nekoyume.State;
using UnityEngine;
using UnityEngine.Networking;
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

        [SerializeField]
        private RawImage image;

        private const string bucketUrl =
            "https://9c-asset-bundle.s3.us-east-2.amazonaws.com/Images/Banner_";

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

            StartCoroutine(SetTexture());
        }

        private IEnumerator SetTexture()
        {
            var split = gameObject.name.Split('_');
            var index = split[^1].Replace("(Clone)", string.Empty);
            var www = UnityWebRequestTexture.GetTexture($"{bucketUrl}{index}.png");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                var myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                image.texture = myTexture;
            }
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
