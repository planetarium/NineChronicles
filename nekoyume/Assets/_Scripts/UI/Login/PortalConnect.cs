using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Libplanet.Crypto;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Nekoyume.UI
{
    public class PortalConnect
    {
        [Serializable]
        public class AccessTokenResult
        {
            public string accessToken;
        }

        [Serializable]
        public class RequestPledgeResult
        {
            public string title;
            public string message;
            public string code;
            public string txId;
        }

        private System.Action _onPortalEnd;
        private string deeplinkURL;

        private string clientSecret;
        private string code;
        private string accessToken;
        private string txId;

        private readonly string portalUrl;
        private const string RequestPledgeEndpoint = "/api/account/mobile/contract";
        private const string AccessTokenEndpoint = "/api/auth/token";
        private const int Timeout = 30;

        public PortalConnect(string url)
        {
            portalUrl = url ?? throw new ArgumentNullException(nameof(url));

            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                OnDeepLinkActivated(Application.absoluteURL);
            }
            else deeplinkURL = "[none]";
        }

        public void OpenPortal(System.Action onPortalEnd = null)
        {
            _onPortalEnd = onPortalEnd;

            clientSecret = GenerateClientSecret();
            Application.OpenURL($"{portalUrl}/mobile-signin?clientSecret={clientSecret}");
        }

        private void OnDeepLinkActivated(string url)
        {
            deeplinkURL = url;

            if (_onPortalEnd != null)
            {
                _onPortalEnd();
                _onPortalEnd = null;
            }

            var param = deeplinkURL.Split('?')[1].Split('&')
                .ToDictionary(str => str.Split('=')[0], str => str.Split('=')[1]);

            if (param.ContainsKey("clientSecret"))
            {
                if (!clientSecret.Equals(param["clientSecret"]))
                {
                    Debug.LogError($"clientSecret is not matched. {clientSecret} != {param["clientSecret"]}");
                    return;
                }
            }

            if (param.ContainsKey("code"))
            {
                code = param["code"];
            }

            Address? address = param.ContainsKey("ncAddress") ? new Address(param["ncAddress"]) : null;
            Widget.Find<LoginSystem>().Show(address);
            AccessToken();
        }

        private static string GenerateClientSecret(int length = 16)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringBuilder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                stringBuilder.Append(chars[Random.Range(0, chars.Length)]);
            }

            return stringBuilder.ToString();
        }

        private async void AccessToken()
        {
            var url = $"{portalUrl}{AccessTokenEndpoint}";

            var form = new WWWForm();
            form.AddField("clientSecret", clientSecret);
            form.AddField("code", code);

            var request = UnityWebRequest.Post(url, form);
            request.timeout = Timeout;

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var json = request.downloadHandler.text;
                var data = JsonUtility.FromJson<AccessTokenResult>(json);
                if (!string.IsNullOrEmpty(data.accessToken))
                {
                    accessToken = data.accessToken;
                }
            }
            else
            {
                Debug.LogError($"AccessToken Error: {request.error}");
            }
        }

        public IEnumerator RequestPledge(Address address)
        {
            var url = $"{portalUrl}{RequestPledgeEndpoint}";
            var os = string.Empty;
#if UNITY_ANDROID
            os = "android";
#elif UNITY_IOS
            os = "ios";
#endif

            var form = new WWWForm();
            form.AddField("address", address.ToHex());
            form.AddField("os", os);

            var request = UnityWebRequest.Post(url, form);
            request.timeout = Timeout;
            request.SetRequestHeader("authorization", $"Bearer {accessToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var json = request.downloadHandler.text;
                var data = JsonUtility.FromJson<RequestPledgeResult>(json);
                if (!string.IsNullOrEmpty(data.txId))
                {
                    txId = data.txId;
                }
                else
                {
                    Debug.LogError($"RequestPledge Deserialize Error: {json}");
                }
            }
            else
            {
                var json = request.downloadHandler.text;
                Debug.LogError($"RequestPledge Error: {request.error}\n{json}");
            }
        }
    }
}
