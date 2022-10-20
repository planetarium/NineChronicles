using System;
using System.Collections;
using Nekoyume.State;
using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EventBannerItem : MonoBehaviour
    {
        [SerializeField]
        private RawImage image;

        [SerializeField]
        private Button button;

        private const string Url =
            "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/main/Assets/Images/Banner";

        public void Set(EventBannerData data)
        {
            StartCoroutine(SetTexture(data.ImageName));
            SetButton(data.Url, data.UseAgentAddress);
        }

        private IEnumerator SetTexture(string imageName)
        {
            var www = UnityWebRequestTexture.GetTexture($"{Url}/{imageName}.png");
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

        private void SetButton(string url, bool useAgentAddress)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
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
    }
}
