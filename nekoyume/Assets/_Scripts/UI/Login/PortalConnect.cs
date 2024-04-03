using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume.Game.OAuth;
using Nekoyume.Helper;
using Nekoyume.Multiplanetary;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Nekoyume.UI
{
    using System.Net.Http;
    using Nekoyume.Blockchain;
    using UniRx;
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
            public float inviterReward;
            public float inviteeReward;
            public int requiredLevel;
            public float inviteeLevelReward;
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
        private const string RefreshTokenEndpoint = "/api/auth/mobile/refresh";
        private const string ReferralEndpoint = "/api/invitations/mobile/referral";

        private const string PortalRewardEndpoint = "/earn#Play";
        private const string ClientSecretKey = "Cached_ClientSecret";
        private const int Timeout = 180;

        public bool HasAccessToken => !string.IsNullOrEmpty(accessToken);

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

            NcDebug.Log($"[{nameof(PortalConnect)}] constructed: PortalUrl({PortalUrl})" +
                      $", deeplinkURL({deeplinkURL})" +
                      $", accessToken({accessToken})" +
                      $", refreshToken({refreshToken})");
        }

        public void OpenPortal(System.Action onPortalEnd = null)
        {
            var url = $"{PortalUrl}/mobile-signin?clientSecret={clientSecret}";
            NcDebug.Log($"[{nameof(PortalConnect)}] {nameof(OpenPortal)} invoked: url({url})");
            _onPortalEnd = onPortalEnd;

            clientSecret = GetClientSecret();
            Application.OpenURL(url);
            Analyzer.Instance.Track("Unity/Portal/1");

            var evt = new AirbridgeEvent("Portal_1");
            AirbridgeUnity.TrackEvent(evt);
        }

        public void OpenPortalRewardUrl()
        {
            var url = $"{PortalUrl}{PortalRewardEndpoint}";
            NcDebug.Log($"[{nameof(PortalConnect)}] {nameof(OpenPortalRewardUrl)} invoked: url({url})");
            Application.OpenURL(url);
        }

        private void OnDeepLinkActivated(string url)
        {
            NcDebug.Log($"[{nameof(PortalConnect)}] {nameof(OnDeepLinkActivated)} invoked: url({url})");
            deeplinkURL = url;

            if (_onPortalEnd != null)
            {
                _onPortalEnd();
                _onPortalEnd = null;
            }

            if (KeyManager.Instance.GetListIds().Any())
            {
                return;
            }

            Analyzer.Instance.Track("Unity/Portal/2");

            var evt = new AirbridgeEvent("Portal_2");
            AirbridgeUnity.TrackEvent(evt);

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

            var evt = new AirbridgeEvent("Portal_3");
            AirbridgeUnity.TrackEvent(evt);

            var url = $"{PortalUrl}{RequestCodeEndpoint}?clientSecret={clientSecret}";
            NcDebug.Log($"[{nameof(PortalConnect)}] {nameof(RequestCode)} invoked: url({url})");

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
                    NcDebug.LogError($"[{nameof(PortalConnect)}] {nameof(RequestCode)} Deserialize Error: {json}");
                    ShowRequestErrorPopup(data);
                }
            }
            else
            {
                NcDebug.LogError($"[{nameof(PortalConnect)}] {nameof(RequestCode)} " +
                               $"Error: {request.error}\n{json}\nclientSecret: {clientSecret}");
                ShowRequestErrorPopup(data);
            }
        }

        private async void GetAccessToken()
        {
            var url = $"{PortalUrl}{AccessTokenEndpoint}";
            NcDebug.Log($"[{nameof(PortalConnect)}] {nameof(GetAccessToken)} invoked: " +
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
                NcDebug.LogException(e);
            }

            HandleTokensResult(request);
            // Set RefreshToken To PlayerPrefs
        }

        private async Task<bool> UpdateTokens()
        {
            var url = $"{PortalUrl}{RefreshTokenEndpoint}";

            NcDebug.Log($"[{nameof(PortalConnect)}] {nameof(UpdateTokens)} invoked: url({url}), refreshToken({refreshToken})");

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
                NcDebug.LogException(e);
            }

            if (HandleTokensResult(request))
            {
                return true;
            }

            var data = JsonUtility.FromJson<AccessTokenResult>(request.downloadHandler.text);
            if (data.resultCode is 3003 or 3004)
            {
                var (_, _, address) = await GetTokensSilentlyAsync();
                return address is not null;
            }

            return false;
        }

        public async Task<(
            string email,
            string idToken,
            Address? address)> GetTokensSilentlyAsync()
        {
            switch (SigninContext.LatestSignedInSocialType)
            {
                case SigninContext.SocialType.Google:
                    return await ProcessGoogleSigningSilently();
                case SigninContext.SocialType.Apple:
                    return await ProcessAppleSigningSilently();
                default:
                    return await ProcessGoogleSigningSilently();
                // case null:
                //     Debug.LogError(
                //         $"[{nameof(PortalConnect)}] {nameof(GetTokensSilentlyAsync)}... " +
                //         "SigninContext.LatestSignedInSocialType is null.");
                //     return false;
                // default:
                //     throw new ArgumentOutOfRangeException();
            }
        }

        private async Task<(
            string email,
            string idToken,
            Address? address)> ProcessGoogleSigningSilently()
        {
            if (!Game.Game.instance.TryGetComponent<GoogleSigninBehaviour>(out var google))
            {
                google = Game.Game.instance.gameObject.AddComponent<GoogleSigninBehaviour>();
            }

            var logTitle = $"[{nameof(PortalConnect)}] {nameof(ProcessGoogleSigningSilently)}";
            NcDebug.Log($"{logTitle} invoked: google.State.Value({google.State.Value})");

            switch (google.State.Value)
            {
                case GoogleSigninBehaviour.SignInState.Signed:
                    NcDebug.Log($"{logTitle}... Already signed in google. Anyway, invoke SendGoogleIdToken.");
                    SigninContext.SetLatestSignedInSocialType(SigninContext.SocialType.Google);
                    return (
                        email: google.Email,
                        idToken: google.IdToken,
                        address: await SendGoogleIdTokenAsync(google.IdToken));
                case GoogleSigninBehaviour.SignInState.Waiting:
                    NcDebug.Log($"{logTitle}... Already waiting for google sign in.");
                    return (
                        email: google.Email,
                        idToken: google.IdToken,
                        address: null);
                case GoogleSigninBehaviour.SignInState.Undefined:
                case GoogleSigninBehaviour.SignInState.Canceled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            google.OnSignInSilently();
            var state = await google.State.SkipLatestValueOnSubscribe().First();

            switch (state)
            {
                case GoogleSigninBehaviour.SignInState.Undefined:
                case GoogleSigninBehaviour.SignInState.Waiting:
                    return (
                        email: google.Email,
                        idToken: google.IdToken,
                        address: null);
                case GoogleSigninBehaviour.SignInState.Canceled:
                    break;
                case GoogleSigninBehaviour.SignInState.Signed:
                    SigninContext.SetLatestSignedInSocialType(SigninContext.SocialType.Google);
                    return (
                        email: google.Email,
                        idToken: google.IdToken,
                        address: await SendGoogleIdTokenAsync(google.IdToken));
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }

            return (
                email: google.Email,
                idToken: google.IdToken,
                address: null);
        }

        private async Task<(
            string email,
            string idToken,
            Address? address)> ProcessAppleSigningSilently()
        {
            // Todo: Apple Signin (currently, not invoke signin)

            if (!Game.Game.instance.TryGetComponent<AppleSigninBehaviour>(out var apple))
            {
                apple = Game.Game.instance.gameObject.AddComponent<AppleSigninBehaviour>();
                apple.Initialize();
            }

            var logTitle = $"[{nameof(PortalConnect)}] {nameof(ProcessAppleSigningSilently)}";
            NcDebug.Log($"{logTitle} invoked: apple.State.Value({apple.State.Value})");

            switch (apple.State.Value)
            {
                case AppleSigninBehaviour.SignInState.Signed:
                    SigninContext.SetLatestSignedInSocialType(SigninContext.SocialType.Apple);
                    NcDebug.Log($"{logTitle}... Already signed in apple. Anyway, invoke SendAppleIdToken.");
                    return (
                        email: apple.Email,
                        idToken: apple.IdToken,
                        address: await SendAppleIdTokenAsync(apple.IdToken));
                case AppleSigninBehaviour.SignInState.Waiting:
                    NcDebug.Log($"{logTitle}... Already waiting for apple sign in.");
                    return (
                        email: apple.Email,
                        idToken: apple.IdToken,
                        address: null);
                case AppleSigninBehaviour.SignInState.Undefined:
                case AppleSigninBehaviour.SignInState.Canceled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            apple.OnSignIn();

            var state = await apple.State.SkipLatestValueOnSubscribe().First();
            switch (state)
            {
                case AppleSigninBehaviour.SignInState.Undefined:
                case AppleSigninBehaviour.SignInState.Waiting:
                    return (
                        email: apple.Email,
                        idToken: apple.IdToken,
                        address: null);
                case AppleSigninBehaviour.SignInState.Canceled:
                    break;
                case AppleSigninBehaviour.SignInState.Signed:
                    SigninContext.SetLatestSignedInSocialType(SigninContext.SocialType.Apple);
                    return (
                        email: apple.Email,
                        idToken: apple.IdToken,
                        address: await SendAppleIdTokenAsync(apple.IdToken));
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }

            return (
                email: apple.Email,
                idToken: apple.IdToken,
                address: null);
        }

        private bool GetRefreshTokenFromPlayerPrefs(string address)
        {
            var encryptedRefreshToken = PlayerPrefs.GetString($"LOCAL_REFRESH_TOKEN_{address}");
            if (string.IsNullOrEmpty(encryptedRefreshToken) ||
                string.IsNullOrWhiteSpace(encryptedRefreshToken))
            {
                return false;
            }

            var decryptedRefreshToken = Util.AesDecrypt(encryptedRefreshToken);
            if (string.IsNullOrEmpty(decryptedRefreshToken) ||
                string.IsNullOrWhiteSpace(decryptedRefreshToken))
            {
                return false;
            }

            refreshToken = decryptedRefreshToken;
            return true;
        }

        private void SetRefreshTokenToPlayerPrefs(string address)
        {
            PlayerPrefs.SetString($"LOCAL_REFRESH_TOKEN_{address}", Util.AesEncrypt(refreshToken));
            PlayerPrefs.Save();
        }

        public async Task<Address?> SendGoogleIdTokenAsync(string idToken)
        {
            NcDebug.Log($"[GoogleSigninBehaviour] CoSendGoogleIdToken invoked w/ idToken({idToken})");
            Analyzer.Instance.Track("Unity/Intro/GoogleSignIn/ConnectToPortal");

            var evt = new AirbridgeEvent("Intro_GoogleSignIn_ConnectToPortal");
            AirbridgeUnity.TrackEvent(evt);

            var body = new JsonObject {{"idToken", idToken}};
            var bodyString = body.ToJsonString(new JsonSerializerOptions {WriteIndented = true});
            var request = new UnityWebRequest($"{PortalUrl}{GoogleAuthEndpoint}", "POST");
            var jsonToSend = new UTF8Encoding().GetBytes(bodyString);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 180;
            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("accept", "application/json");
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest();

            if (HandleTokensResult(request))
            {
                Analyzer.Instance.Track("Unity/Intro/GoogleSignIn/ConnectedToPortal");

                var loginEvt = new AirbridgeEvent("Login");
                AirbridgeUnity.TrackEvent(loginEvt);

                var accessTokenResult = JsonUtility.FromJson<AccessTokenResult>(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(accessTokenResult.address))
                {
                    var address = new Address(accessTokenResult.address);
                    NcDebug.Log($"[GoogleSigninBehaviour] SendGoogleIdToken succeeded. AgentAddress: {address}");
                    return address;
                }
            }
            else
            {
                NcDebug.LogError($"[GoogleSigninBehaviour] SendGoogleIdToken failed w/ error: {request.error}");
            }

            return null;
        }

        public async Task<Address?> SendAppleIdTokenAsync(string idToken)
        {
            NcDebug.Log($"[AppleSigninBehaviour] CoSendAppleIdToken invoked w/ idToken({idToken})");
            Analyzer.Instance.Track("Unity/Intro/AppleSignIn/ConnectToPortal");

            var evt = new AirbridgeEvent("Intro_AppleSignIn_ConnectToPortal");
            AirbridgeUnity.TrackEvent(evt);

            var body = new JsonObject {{"idToken", idToken}};
            var bodyString = body.ToJsonString(new JsonSerializerOptions {WriteIndented = true});
            var request = new UnityWebRequest($"{PortalUrl}{AppleAuthEndpoint}", "POST");
            var jsonToSend = new UTF8Encoding().GetBytes(bodyString);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 180;
            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("accept", "application/json");
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest();

            if (HandleTokensResult(request))
            {
                Analyzer.Instance.Track("Unity/Intro/AppleSignIn/ConnectedToPortal");

                var loginEvt = new AirbridgeEvent("Login");
                AirbridgeUnity.TrackEvent(loginEvt);

                var accessTokenResult = JsonUtility.FromJson<AccessTokenResult>(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(accessTokenResult.address))
                {
                    var address = new Address(accessTokenResult.address);
                    NcDebug.Log($"[AppleSigninBehaviour] SendAppleleIdToken succeeded. AgentAddress: {address}");
                    return address;
                }
            }
            else
            {
                NcDebug.LogError($"[AppleSigninBehaviour] SendAppleleIdToken failed w/ error: {request.error}");
            }

            return null;
        }

        public async Task<bool> CheckTokensAsync(Address address)
        {
            if (!string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken))
            {
                SetRefreshTokenToPlayerPrefs(address.ToString());
                return true;
            }

            if (GetRefreshTokenFromPlayerPrefs(address.ToString()))
            {
                await UpdateTokens();
                SetRefreshTokenToPlayerPrefs(address.ToString());
                return true;
            }

            var (_, _, addressInPortal) = await GetTokensSilentlyAsync();
            if (addressInPortal is not null)
            {
                if (addressInPortal != address)
                {
                    NcDebug.LogError($"[{nameof(PortalConnect)}] {nameof(CheckTokensAsync)}... " +
                                   $"addressInPortal({addressInPortal}) != address({address})");
                    return false;
                }

                SetRefreshTokenToPlayerPrefs(address.ToString());
                return true;
            }

            return false;
        }

        public bool HandleTokensResult(UnityWebRequest request)
        {
            var logTitle = $"[{nameof(PortalConnect)}] {nameof(HandleTokensResult)}";

            var json = request.downloadHandler.text;
            NcDebug.Log($"{logTitle} invoked w/ request: result({request.result}), json({json})");
            var data = JsonUtility.FromJson<AccessTokenResult>(json);
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (!string.IsNullOrEmpty(data.accessToken) && !string.IsNullOrEmpty(data.refreshToken))
                {
                    NcDebug.Log($"{logTitle} Success: {json}");
                    accessToken = data.accessToken;
                    refreshToken = data.refreshToken;

                    return true;
                }

                NcDebug.LogError($"{logTitle} Deserialize Error: {json}");
                ShowRequestErrorPopup(data);
            }
            else if (data.resultCode is 3003 or 3004)
            {
                NcDebug.Log($"{logTitle} Refresh Token expired: Refresh Token({accessToken})\n{json}");
            }
            else
            {
                NcDebug.LogError($"{logTitle} Failed: {request.error}\ncode: {code}\nclientSecret: {clientSecret}");
                ShowRequestErrorPopup(data);
            }

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

            NcDebug.Log($"[{nameof(PortalConnect)}] {nameof(RequestPledge)} invoked: " +
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
                    NcDebug.Log($"[{nameof(PortalConnect)}] {nameof(RequestPledge)} Success: {json}");
                    txId = data.txId;
                    PlayerPrefs.DeleteKey(ClientSecretKey);
                }
                else
                {
                    NcDebug.LogError($"[{nameof(PortalConnect)}] {nameof(RequestPledge)} Deserialize Error: {json}");
                    ShowRequestErrorPopup(data);
                }
            }
            else
            {
                NcDebug.LogError($"[{nameof(PortalConnect)}] {nameof(RequestPledge)} Error: " +
                               $"{request.error}\n{json}\naddress: {address.ToHex()}\nos: {os}");
                ShowRequestErrorPopup(data);
            }
        }

        public async Task<ReferralResult> GetReferralInformation()
        {
            var logTitle = $"[{nameof(PortalConnect)}] {nameof(GetReferralInformation)}";
            var url = $"{PortalUrl}{ReferralEndpoint}";

            NcDebug.Log($"{logTitle} invoked: url({url}), accessToken({accessToken})");

#if UNITY_IOS
            var client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("authorization", $"Bearer {accessToken}");

            var resp = await client.SendAsync(req);
            var json = await resp.Content.ReadAsStringAsync();
            var data = JsonUtility.FromJson<ReferralResult>(json);
            if (resp.IsSuccessStatusCode)
            {
                if (!string.IsNullOrEmpty(data.referralCode))
                {
                    Debug.Log($"{logTitle} Success: {json}");
                    return data;
                }

                Debug.LogError($"{logTitle} Deserialize Error: {json}");
                ShowRequestErrorPopup(data);
            }
            else if (data.resultCode is 3001 or 3002)
            {
                Debug.Log($"{logTitle} Access Token expired: Access Token({accessToken})\n{json}");
                if (await UpdateTokens())
                {
                    return await GetReferralInformation();
                }
            }
            else
            {
                Debug.LogError($"{logTitle} Failed: {resp.StatusCode}\n{json}\n");
                ShowRequestErrorPopup(data);
            }
#else
            var request = UnityWebRequest.Get(url);
            request.timeout = Timeout;
            request.SetRequestHeader("authorization", $"Bearer {accessToken}");

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException e)
            {
                NcDebug.LogException(e);
            }

            var json = request.downloadHandler.text;
            var data = JsonUtility.FromJson<ReferralResult>(json);
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (!string.IsNullOrEmpty(data.referralCode))
                {
                    NcDebug.Log($"{logTitle} Success: {json}");
                    return data;
                }

                NcDebug.LogError($"{logTitle} Deserialize Error: {json}");
                ShowRequestErrorPopup(data);
            }
            else if (data.resultCode is 3001 or 3002)
            {
                NcDebug.Log($"{logTitle} Access Token expired: Access Token({accessToken})\n{json}");
                if (await UpdateTokens())
                {
                    return await GetReferralInformation();
                }
            }
            else
            {
                NcDebug.LogError($"{logTitle} Failed: {request.error}\n{json}\n");
                ShowRequestErrorPopup(data);
            }
#endif

            return null;
        }

        public async Task<RequestResult> EnterReferralCode(string referralCode)
        {
            var logTitle = $"[{nameof(PortalConnect)}] {nameof(EnterReferralCode)}";
            var url = $"{PortalUrl}{ReferralEndpoint}";

            NcDebug.Log($"{logTitle} invoked: url({url}), referralCode({referralCode}) accessToken({accessToken})");

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
                NcDebug.LogException(e);
            }

            var json = request.downloadHandler.text;
            var data = JsonUtility.FromJson<RequestResult>(json);
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 200)
                {
                    NcDebug.Log($"{logTitle} Success: {json}");
                    return null;
                }
            }
            else if (data.resultCode is 3001 or 3002)
            {
                NcDebug.Log($"{logTitle} Access Token expired: Access Token({accessToken})\n{json}");
                if (await UpdateTokens())
                {
                    return await EnterReferralCode(referralCode);
                }
            }
            else
            {
                NcDebug.LogError($"{logTitle} Error: {request.error}\n{json}\n");
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

            var evt = new AirbridgeEvent("Portal_0");
            AirbridgeUnity.TrackEvent(evt);
        }
    }
}
