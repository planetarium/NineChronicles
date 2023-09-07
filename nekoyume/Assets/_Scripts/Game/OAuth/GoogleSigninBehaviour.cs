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
        private string _idToken = string.Empty;

        public void OnSignIn()
        {
            GoogleSignIn.Configuration = _configuration;
            Debug.Log("Calling SignIn");
            State.Value = SignInState.Waiting;
            GoogleSignIn.DefaultInstance.SignIn()
                .ContinueWith(OnAuthenticationFinished);
            State.SkipLatestValueOnSubscribe().First().Subscribe(state =>
            {
                if (state is SignInState.Signed)
                {
                    StartCoroutine(CoSendGoogleIdToken(_idToken));
                }
            });
        }

        private void OnSignInSilently()
        {
            GoogleSignIn.Configuration = _configuration;
            Debug.Log("Calling SignIn Silently");
            GoogleSignIn.DefaultInstance.SignInSilently()
                .ContinueWith(OnAuthenticationFinished);
        }

        public void OnSignOut()
        {
            Debug.Log("Calling SignOut");
            GoogleSignIn.DefaultInstance.SignOut();
        }

        public void OnDisconnect()
        {
            Debug.Log("Calling Disconnect");
            GoogleSignIn.DefaultInstance.Disconnect();
        }

        private void OnAuthenticationFinished(Task<GoogleSignInUser> task)
        {
            if (task.IsFaulted)
            {
                using var enumerator =
                    task.Exception?.InnerExceptions.GetEnumerator();
                if (enumerator != null && enumerator.MoveNext())
                {
                    var error = (GoogleSignIn.SignInException)enumerator.Current;
                    Debug.Log("Got Error: " + error.Status + " " + error.Message);
                }
                else
                {
                    Debug.Log("Got Unexpected Exception?!?" + task.Exception);
                }
            }
            else if (task.IsCanceled)
            {
                Debug.Log("Canceled");
                State.Value = SignInState.Canceled;
            }
            else
            {
                var res = task.Result;
                Debug.Log("Welcome: " + res.UserId + "!" + $"\ntoken: {res.IdToken}");
                _idToken = res.IdToken;
                State.Value = SignInState.Signed;
            }
        }

        private IEnumerator CoSendGoogleIdToken(string idToken)
        {
            var body = new JsonObject {{"idToken", idToken}};
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

            if (Game.instance.PortalConnect.HandleAccessTokenResult(request))
            {
                var accessTokenResult =
                    JsonUtility.FromJson<PortalConnect.AccessTokenResult>(
                        request.downloadHandler.text);
                Address? address = null;
                if (!string.IsNullOrEmpty(accessTokenResult.address))
                {
                    address = new Address(accessTokenResult.address);
                }

                Widget.Find<LoginSystem>().Show(address);
            }
        }
    }
}
