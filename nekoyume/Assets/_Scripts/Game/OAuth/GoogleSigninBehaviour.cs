using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Google;
using Libplanet.Crypto;
using Nekoyume.UI;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.Game.OAuth
{
    public class GoogleSigninBehaviour : MonoBehaviour
    {
        public enum SignInState
        {
            NotSigned,
            Signed,
            Canceled,
            Waiting
        }

        public ReactiveProperty<SignInState> State { get; } = new(SignInState.NotSigned);

        private const string WebClientId =
            "449111430622-hu1uin72e3n3727rmab7e9sslbvnimrr.apps.googleusercontent.com";

        private readonly GoogleSignInConfiguration _configuration = new()
        {
            WebClientId = WebClientId,
            RequestIdToken = true,
            RequestEmail = true,
            RequestAuthCode = true,
            RequestProfile = true,
            UseGameSignIn = false
        };

        public string IDToken { get; private set; } = string.Empty;
        public Address? AgentAddress { get; private set; } = null;

        public void OnSignIn()
        {
            Debug.Log("[GoogleSigninBehaviour] OnSignIn() invoked.");
            GoogleSignIn.Configuration = _configuration;
            State.Value = SignInState.Waiting;
            GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnAuthenticationFinished);
            State.SkipLatestValueOnSubscribe().First().Subscribe(state =>
            {
                Debug.Log($"[GoogleSigninBehaviour] State changed: {state}");
                if (state is SignInState.Signed)
                {
                    Analyzer.Instance.Track("Unity/Intro/GoogleSignIn/Signed");
                }

                if (state is SignInState.Canceled)
                {
                    Analyzer.Instance.Track("Unity/Intro/GoogleSignIn/Canceled");
                }
            });
        }

        private void OnSignInSilently()
        {
            Debug.Log("[GoogleSigninBehaviour] OnSignInSilently() invoked.");
            GoogleSignIn.Configuration = _configuration;
            GoogleSignIn.DefaultInstance.SignInSilently()
                .ContinueWith(OnAuthenticationFinished);
        }

        public void OnSignOut()
        {
            Debug.Log("[GoogleSigninBehaviour] OnSignOut() invoked.");
            GoogleSignIn.DefaultInstance.SignOut();
        }

        public void OnDisconnect()
        {
            Debug.Log("[GoogleSigninBehaviour] OnDisconnect() invoked.");
            GoogleSignIn.DefaultInstance.Disconnect();
        }

        private void OnAuthenticationFinished(Task<GoogleSignInUser> task)
        {
            Debug.Log("[GoogleSigninBehaviour] OnAuthenticationFinished() invoked.");
            if (task.IsFaulted)
            {
                Debug.LogError("[GoogleSigninBehaviour] OnAuthenticationFinished()..." +
                               " task is faulted.");
                using var enumerator =
                    task.Exception?.InnerExceptions.GetEnumerator();
                if (enumerator != null &&
                    enumerator.MoveNext() &&
                    enumerator.Current is GoogleSignIn.SignInException e)
                {
                    Debug.LogException(e);
                }
                else
                {
                    Debug.LogError("[GoogleSigninBehaviour] OnAuthenticationFinished()..." +
                                   " unexpected exception occurred.");
                    Debug.LogException(task.Exception);
                }

                State.Value = SignInState.Canceled;
            }
            else if (task.IsCanceled)
            {
                Debug.Log("[GoogleSigninBehaviour] OnAuthenticationFinished()... task is canceled.");
                State.Value = SignInState.Canceled;
            }
            else
            {
                var res = task.Result;
                Debug.Log("[GoogleSigninBehaviour] OnAuthenticationFinished()..." +
                          $" Welcome: {res.UserId}!\ntoken: {res.IdToken}");
                IDToken = res.IdToken;
                State.Value = SignInState.Signed;
            }
        }

        public IEnumerator CoSendGoogleIdToken()
        {
            Debug.Log($"[GoogleSigninBehaviour] CoSendGoogleIdToken invoked w/ idToken({IDToken})");
            yield return new WaitUntil(() => Game.instance.PortalConnect != null);
            Analyzer.Instance.Track("Unity/Intro/GoogleSignIn/ConnectToPortal");

            var body = new JsonObject {{"idToken", IDToken}};
            var bodyString = body.ToJsonString(new JsonSerializerOptions {WriteIndented = true});
            var request =
                new UnityWebRequest(
                    $"{Game.instance.PortalConnect.PortalUrl}{PortalConnect.GoogleAuthEndpoint}",
                    "POST");
            var jsonToSend = new System.Text.UTF8Encoding().GetBytes(bodyString);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 180;
            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("accept", "application/json");
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (Game.instance.PortalConnect.HandleTokensResult(request))
            {
                Analyzer.Instance.Track("Unity/Intro/GoogleSignIn/ConnectedToPortal");
                var accessTokenResult =
                    JsonUtility.FromJson<PortalConnect.AccessTokenResult>(
                        request.downloadHandler.text);
                if (!string.IsNullOrEmpty(accessTokenResult.address))
                {
                    AgentAddress = new Address(accessTokenResult.address);
                    Debug.Log("[GoogleSigninBehaviour] CoSendGoogleIdToken succeeded." +
                              $" AgentAddress: {AgentAddress}");
                }
            }
            else
            {
                Debug.LogError(
                    $"[GoogleSigninBehaviour] CoSendGoogleIdToken failed w/ error: {request.error}");
            }
        }
    }
}
