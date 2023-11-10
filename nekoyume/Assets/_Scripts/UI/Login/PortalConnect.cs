using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume.Helper;
using Nekoyume.Planet;
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
            public int resultCode;
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
            public string refreshToken;
            public string address;
        }

        [Serializable]
        public class RequestPledgeResult : RequestResult
        {
            public string txId;
        }

        [Serializable]
        public class ReferralResult : RequestResult
        {
            public string referralCode;
            public int inviterReward;
            public int inviteeReward;
            public int requiredLevel;
            public int inviteeLevelReward;
            public bool isRegistered;
            public string referralUrl;
        }

        private System.Action _onPortalEnd;
        private string deeplinkURL;

        private string clientSecret;
        private string code;
        private string accessToken;
        private string refreshToken;
        private string txId;
        private ReferralResult referralResult;

        public readonly string PortalUrl;
        public const string GoogleAuthEndpoint = "/api/auth/login/google";
        public const string AppleAuthEndpoint = "/api/auth/login/apple";
        private const string RequestCodeEndpoint = "/api/auth/code";
        private const string RequestPledgeEndpoint = "/api/account/mobile/contract";
        private const string AccessTokenEndpoint = "/api/auth/token";
        private const string RefreshTokenEndpoint = "api/auth/mobile/refresh";
        private const string ReferralEndpoint = "/api/invitations/mobile/referral";

        private const string PortalRewardEndpoint = "/earn#Play";
        private const string ClientSecretKey = "Cached_ClientSecret";
        private const int Timeout = 180;

        public PortalConnect(string url)
        {
            PortalUrl = url ?? "https://nine-chronicles.com";

            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                OnDeepLinkActivated(Application.absoluteURL);
            }
            else
            {
                deeplinkURL = "[none]";
            }

            Debug.Log($"[{nameof(PortalConnect)}] constructed: PortalUrl({PortalUrl}), deeplinkURL({deeplinkURL})");
        }

        public void OpenPortal(System.Action onPortalEnd = null)
        {
            var url = $"{PortalUrl}/mobile-signin?clientSecret={clientSecret}";
            Debug.Log($"[{nameof(PortalConnect)}] {nameof(OpenPortal)} invoked: url({url})");
            _onPortalEnd = onPortalEnd;

            clientSecret = GetClientSecret();
            Application.OpenURL(url);
            Analyzer.Instance.Track("Unity/Portal/1");
        }

        public void OpenPortalRewardUrl()
        {
            var url = $"{PortalUrl}{PortalRewardEndpoint}";
            Debug.Log($"[{nameof(PortalConnect)}] {nameof(OpenPortalRewardUrl)} invoked: url({url})");
            Application.OpenURL(url);
        }

        private void OnDeepLinkActivated(string url)
        {
            Debug.Log($"[{nameof(PortalConnect)}] {nameof(OnDeepLinkActivated)} invoked: url({url})");
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

            if (param.TryGetValue("clientSecret", out var outClientSecret))
            {
                clientSecret = outClientSecret;
            }

            var accountExist = param.ContainsKey("ncAddress");
            if (param.TryGetValue("code", out var outCode))
            {
                code = outCode;
                if (string.IsNullOrEmpty(code) && !accountExist)
                {
                    RequestCode(OnSuccess);
                    return;
                }
            }

            OnSuccess();
            return;

            void OnSuccess()
            {
                if (!accountExist)
                {
                    GetAccessToken();
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

        private async void RequestCode(System.Action onSuccess)
        {
            Analyzer.Instance.Track("Unity/Portal/3");

            var url = $"{PortalUrl}{RequestCodeEndpoint}?clientSecret={clientSecret}";
            Debug.Log($"[{nameof(PortalConnect)}] {nameof(RequestCode)} invoked: url({url})");

            var form = new WWWForm();
            var request = UnityWebRequest.Post(url, form);
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
                    Debug.LogError($"[{nameof(PortalConnect)}] {nameof(RequestCode)} Deserialize Error: {json}");
                    ShowRequestErrorPopup(data);
                }
            }
            else
            {
                Debug.LogError($"[{nameof(PortalConnect)}] {nameof(RequestCode)} " +
                               $"Error: {request.error}\n{json}\nclientSecret: {clientSecret}");
                ShowRequestErrorPopup(data);
            }
        }

        private async void GetAccessToken()
        {
            var url = $"{PortalUrl}{AccessTokenEndpoint}";
            Debug.Log($"[{nameof(PortalConnect)}] {nameof(GetAccessToken)} invoked: " +
                      $"url({url}), clientSecret({clientSecret}), code({code})");

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
                Debug.LogException(e);
            }

            HandleTokensResult(request);
        }

        private async Task<bool> RefreshTokens()
        {
            var url = $"{PortalUrl}{RefreshTokenEndpoint}";

            Debug.Log($"[{nameof(PortalConnect)}] {nameof(RefreshTokens)} invoked: " +
                      $"url({url}), refreshToken({refreshToken})");

            var form = new WWWForm();
            form.AddField("refreshToken", refreshToken);

            var request = UnityWebRequest.Post(url, form);
            request.timeout = Timeout;

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException e)
            {
                Debug.LogException(e);
            }

            return HandleTokensResult(request);
        }

        private async void GetRefreshToken()
        {
            // Social Login
        }

        private bool GetTokens(string address)
        {
            var encryptedRefreshToken = PlayerPrefs.GetString($"LOCAL_REFRESH_TOKEN_{address}", string.Empty);

            if (string.IsNullOrEmpty(encryptedRefreshToken))
            {
                return false;
            }

            refreshToken = Util.AesDecrypt(encryptedRefreshToken);
            return true;
        }

        private void SetTokens(string address)
        {
            PlayerPrefs.SetString($"LOCAL_REFRESH_TOKEN_{address}", Util.AesEncrypt(refreshToken));
            PlayerPrefs.Save();
        }

        public void RefreshTokens(Address address)
        {
            if (GetTokens(address.ToString()))
            {
                RefreshTokens();
                return;
            }
            GetRefreshToken();
        }

        public bool HandleTokensResult(UnityWebRequest request)
        {
            var json = request.downloadHandler.text;
            Debug.Log($"[{nameof(PortalConnect)}] {nameof(HandleTokensResult)} invoked w/ request: " +
                      $"result({request.result}), json({json})");
            var data = JsonUtility.FromJson<AccessTokenResult>(json);
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 200)
                {
                    accessToken = data.accessToken;
                    refreshToken = data.refreshToken;
                    SetTokens(Game.Game.instance.Agent.Address.ToString());
                    return true;
                }

                if (data.resultCode == 3004)
                {
                    Debug.Log($"[{nameof(PortalConnect)}] {nameof(HandleTokensResult)} Refresh Token expired: " +
                              $"Refresh Token({accessToken})\n{json}");
                    GetRefreshToken();
                }
                else
                {
                    Debug.LogError($"[{nameof(PortalConnect)}] {nameof(HandleTokensResult)}... json deserialize error.");
                    ShowRequestErrorPopup(data);
                }
            }

            Debug.LogError($"[{nameof(PortalConnect)}] {nameof(HandleTokensResult)}... " +
                           $"result failed: {request.error}\ncode: {code}\nclientSecret: {clientSecret}");
            ShowRequestErrorPopup(data);
            return false;
        }

        public IEnumerator RequestPledge(PlanetId planetId, Address address)
        {
            var url = $"{PortalUrl}{RequestPledgeEndpoint}";
            var os = string.Empty;
#if UNITY_ANDROID
            os = "android";
#elif UNITY_IOS
            os = "ios";
#endif

            Debug.Log($"[{nameof(PortalConnect)}] {nameof(RequestPledge)} invoked: " +
                      $"url({url}), os({os}), planetId({planetId}), address({address}), accessToken({accessToken})");

            var form = new WWWForm();
            form.AddField("address", address.ToHex());
            form.AddField("os", os);
            form.AddField("planetId", planetId.ToString());

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
                    Debug.Log($"[{nameof(PortalConnect)}] {nameof(RequestPledge)} Success: {json}");
                    txId = data.txId;
                    PlayerPrefs.DeleteKey(ClientSecretKey);
                }
                else
                {
                    Debug.LogError($"[{nameof(PortalConnect)}] {nameof(RequestPledge)} Deserialize Error: {json}");
                    ShowRequestErrorPopup(data);
                }
            }
            else
            {
                Debug.LogError($"[{nameof(PortalConnect)}] {nameof(RequestPledge)} Error: " +
                               $"{request.error}\n{json}\naddress: {address.ToHex()}\nos: {os}");
                ShowRequestErrorPopup(data);
            }
        }

        public async Task<ReferralResult> GetReferralInformation()
        {
            var url = $"{PortalUrl}{ReferralEndpoint}";

            Debug.Log($"[{nameof(PortalConnect)}] {nameof(GetReferralInformation)} invoked: " +
                      $"url({url}), accessToken({accessToken})");

            var request = UnityWebRequest.Get(url);
            request.timeout = Timeout;
            request.SetRequestHeader("authorization", $"Bearer {accessToken}");

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException e)
            {
                Debug.LogException(e);
            }

            var json = request.downloadHandler.text;
            var data = JsonUtility.FromJson<ReferralResult>(json);
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 200)
                {
                    Debug.Log($"[{nameof(PortalConnect)}] {nameof(GetReferralInformation)} Success: {json}");
                    return data;
                }

                if (data.resultCode == 3002)
                {
                    Debug.Log($"[{nameof(PortalConnect)}] {nameof(GetReferralInformation)} Access Token expired: " +
                              $"Access Token({accessToken})\n{json}");
                    if (await RefreshTokens())
                    {
                        return await GetReferralInformation();
                    }
                }
                else
                {
                    Debug.LogError($"[{nameof(PortalConnect)}] {nameof(GetReferralInformation)} Deserialize Error: {json}");
                    ShowRequestErrorPopup(data);
                }
            }
            else
            {
                Debug.LogError($"[{nameof(PortalConnect)}] {nameof(GetReferralInformation)} Error: {request.error}\n{json}\n");
                ShowRequestErrorPopup(data);
            }

            return null;
        }

        public async Task<RequestResult> EnterReferralCode(string referralCode)
        {
            var url = $"{PortalUrl}{ReferralEndpoint}";

            Debug.Log($"[{nameof(PortalConnect)}] {nameof(EnterReferralCode)} invoked: " +
                      $"url({url}), referralCode({referralCode}) accessToken({accessToken})");

            var form = new WWWForm();
            form.AddField("referralCode", referralCode);

            var request = UnityWebRequest.Post(url, form);
            request.timeout = Timeout;
            request.SetRequestHeader("authorization", $"Bearer {accessToken}");

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException e)
            {
                Debug.LogException(e);
            }

            var json = request.downloadHandler.text;
            var data = JsonUtility.FromJson<RequestResult>(json);
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 200)
                {
                    Debug.Log($"[{nameof(PortalConnect)}] {nameof(EnterReferralCode)} Success: {json}");
                }

                if (data.resultCode == 3002)
                {
                    Debug.Log($"[{nameof(PortalConnect)}] {nameof(EnterReferralCode)} Access Token expired: " +
                              $"Access Token({accessToken})\n{json}");
                    if (await RefreshTokens())
                    {
                        return await EnterReferralCode(referralCode);
                    }
                }
                else
                {
                    Debug.LogError($"[{nameof(PortalConnect)}] {nameof(EnterReferralCode)} Deserialize Error: {json}");
                }
            }
            else
            {
                Debug.LogError($"[{nameof(PortalConnect)}] {nameof(EnterReferralCode)} Error: {request.error}\n{json}\n");
            }

            return data;
        }

        private static void ShowRequestErrorPopup(RequestResult data)
        {
            var message = "An abnormal condition has been identified. Please try again after finishing the app.";
            message += $"\nError code : {data.resultCode}";
            message += string.IsNullOrEmpty(data.message) ? string.Empty : $"\n{data.message}";

            var popup = Widget.Find<TitleOneButtonSystem>();
            popup.Show(data.title, message, "OK", false);
            popup.SubmitCallback = () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            };
            Analyzer.Instance.Track("Unity/Portal/0");
        }
    }
}
