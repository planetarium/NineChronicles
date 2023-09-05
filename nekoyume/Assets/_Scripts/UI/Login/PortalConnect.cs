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
        public class RequestResult
        {
            public string title;
            public string message;
            public string resultCode;
        }

        [Serializable]
        public class RequestCodeResult : RequestResult
        {
            public string code;
        }

        [Serializable]
        public class AccessTokenResult : RequestResult
        {
            public string accessToken;
        }

        [Serializable]
        public class RequestPledgeResult : RequestResult
        {
            public string txId;
        }

        private System.Action _onPortalEnd;
        private string deeplinkURL;

        private string clientSecret;
        private string code;
        private string accessToken;
        private string txId;

        private readonly string portalUrl;
        private const string GoogleAuthEndpoint = "/api/auth/login/google";
        private const string RequestCodeEndpoint = "/api/auth/code";
        private const string RequestPledgeEndpoint = "/api/account/mobile/contract";
        private const string AccessTokenEndpoint = "/api/auth/token";
        private const string ClientSecretKey = "Cached_ClientSecret";
        private const int Timeout = 180;

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

            clientSecret = GetClientSecret();
            Application.OpenURL($"{portalUrl}/mobile-signin?clientSecret={clientSecret}");
            Analyzer.Instance.Track("Unity/Portal/1");
        }

        private void OnDeepLinkActivated(string url)
        {
            deeplinkURL = url;

            if (_onPortalEnd != null)
            {
                _onPortalEnd();
                _onPortalEnd = null;
            }

            if (Widget.Find<LoginSystem>().KeyStore.ListIds().Any())
            {
                return;
            }

            Analyzer.Instance.Track("Unity/Portal/2");

            var param = deeplinkURL.Split('?')[1].Split('&')
                .ToDictionary(str => str.Split('=')[0], str => str.Split('=')[1]);

            if (param.ContainsKey("clientSecret"))
            {
                clientSecret = param["clientSecret"];
            }

            var accountExist = param.ContainsKey("ncAddress");
            if (param.ContainsKey("code"))
            {
                code = param["code"];
                if (string.IsNullOrEmpty(code) && !accountExist)
                {
                    RequestCode(OnSuccess);
                    return;
                }
            }

            OnSuccess();

            void OnSuccess()
            {
                if (!accountExist)
                {
                    AccessToken();
                }

                Address? address = accountExist
                    ? new Address(param["ncAddress"])
                    : null;
                Widget.Find<LoginSystem>().Show(address);
            }
        }

        private static string GetClientSecret()
        {
            if (PlayerPrefs.HasKey(ClientSecretKey))
            {
                return PlayerPrefs.GetString(ClientSecretKey);
            }

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            const int length = 16;
            var stringBuilder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                stringBuilder.Append(chars[Random.Range(0, chars.Length)]);
            }

            var clientSecret = stringBuilder.ToString();
            PlayerPrefs.SetString(ClientSecretKey, clientSecret);
            PlayerPrefs.Save();

            return clientSecret;
        }

        public async void SendGoogleIdToken(string idToken)
        {
            var formData = new List<IMultipartFormSection>
                {new MultipartFormDataSection("idToken", idToken)};

            var request = UnityWebRequest.Post($"{portalUrl}{GoogleAuthEndpoint}", formData);
            request.timeout = Timeout;

            Debug.Log(request.url);
            await request.SendWebRequest();

            var json = request.downloadHandler.text;
            ShowRequestErrorPopup(request.result, json);
            Debug.Log(json);
        }

        private async void RequestCode(System.Action onSuccess)
        {
            Analyzer.Instance.Track("Unity/Portal/3");

            var url = $"{portalUrl}{RequestCodeEndpoint}?clientSecret={clientSecret}";
            var request = UnityWebRequest.Get(url);
            request.timeout = Timeout;

            await request.SendWebRequest();

            var json = request.downloadHandler.text;
            var data = JsonUtility.FromJson<RequestCodeResult>(json);
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (!string.IsNullOrEmpty(data.code))
                {
                    code = data.code;
                    onSuccess.Invoke();
                }
                else
                {
                    Debug.LogError($"AccessToken Deserialize Error: {json}");
                    ShowRequestErrorPopup(data);
                }
            }
            else
            {
                Debug.LogError($"AccessToken Error: {request.error}\n{json}\nclientSecret: {clientSecret}");
                ShowRequestErrorPopup(request.result, request.error);
            }
        }

        private async void AccessToken()
        {
            var url = $"{portalUrl}{AccessTokenEndpoint}";

            var form = new WWWForm();
            form.AddField("clientSecret", clientSecret);
            form.AddField("code", code);

            var request = UnityWebRequest.Post(url, form);
            request.timeout = Timeout;

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException e)
            {
                Debug.Log(e.Text);
            }

            HandleAccessTokenResult(request);
        }

        public bool HandleAccessTokenResult(UnityWebRequest request)
        {
            var json = request.downloadHandler.text;
            var data = JsonUtility.FromJson<AccessTokenResult>(json);
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (!string.IsNullOrEmpty(data.accessToken))
                {
                    accessToken = data.accessToken;
                    return true;
                }

                Debug.LogError($"AccessToken Deserialize Error: {json}");
                ShowRequestErrorPopup(data);
                return false;
            }

            Debug.LogError($"AccessToken Error: {request.error}\n{json}\ncode: {code}\nclientSecret: {clientSecret}");
            ShowRequestErrorPopup(request.result, request.error);
            return false;
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

            var json = request.downloadHandler.text;
            var data = JsonUtility.FromJson<RequestPledgeResult>(json);
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (!string.IsNullOrEmpty(data.txId))
                {
                    txId = data.txId;
                    PlayerPrefs.DeleteKey(ClientSecretKey);
                }
                else
                {
                    Debug.LogError($"RequestPledge Deserialize Error: {json}");
                    ShowRequestErrorPopup(data);
                }
            }
            else
            {
                Debug.LogError($"RequestPledge Error: {request.error}\n{json}\naddress: {address.ToHex()}\nos: {os}");
                ShowRequestErrorPopup(request.result, request.error);
            }
        }

        private void ShowRequestErrorPopup(RequestResult data)
        {
            var message = "An abnormal condition has been identified. Please try again after finishing the app.";
            message += string.IsNullOrEmpty(data.message) ? string.Empty : $"\n{data.message}";
            message += string.IsNullOrEmpty(data.resultCode) ? string.Empty : $"\nResponse code : {data.resultCode}";

            var popup = Widget.Find<TitleOneButtonSystem>();
            popup.Show(data.title, message, "OK", false);
            popup.SubmitCallback = Application.Quit;
            Analyzer.Instance.Track("Unity/Portal/0");
        }

        private void ShowRequestErrorPopup(UnityWebRequest.Result result, string errorMessage)
        {
            var message = "An abnormal condition has been identified. Please try again after finishing the app.";
            message += string.IsNullOrEmpty(errorMessage) ? string.Empty : $"\n{errorMessage}";

            var popup = Widget.Find<TitleOneButtonSystem>();
            popup.Show(result.ToString(), message, "OK", false);
            popup.SubmitCallback = Application.Quit;
            Analyzer.Instance.Track("Unity/Portal/0");
        }
    }
}
