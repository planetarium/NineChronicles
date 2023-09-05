using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Google;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.Game.OAuth
{
    public class GoogleSigninBehaviour : MonoBehaviour
    {
        private const string WebClientId =
            "340841465287-nuft8giaq0q2ci7utp4pi7fag191qc6s.apps.googleusercontent.com";

        private GoogleSignInConfiguration configuration;
        private string _idToken = string.Empty;
        private bool requestAble;

        // Defer the configuration creation until Awake so the web Client ID
        // Can be set via the property inspector in the Editor.
        private void Awake()
        {
            configuration = new GoogleSignInConfiguration
            {
                WebClientId = WebClientId,
                RequestIdToken = true,
                RequestEmail = true,
                RequestAuthCode = true,
                RequestProfile = true,
                UseGameSignIn = false
            };
            OnSignIn();
        }

        public void OnSignIn()
        {
            GoogleSignIn.Configuration = configuration;
            Debug.Log("Calling SignIn");
            GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
                OnAuthenticationFinished);
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
            }
            else
            {
                var res = task.Result;
                Debug.Log("Welcome: " + res.UserId + "!" + $"\ntoken: {res.IdToken}");
                _idToken = res.IdToken;
                requestAble = true;
            }
        }

        private void Update()
        {
            if (requestAble)
            {
                Debug.Log(_idToken);

                StartCoroutine(CoSendGoogleIdToken(_idToken));
                _idToken = string.Empty;
                requestAble = false;
                Debug.Log("send id token");
            }
        }

        private IEnumerator CoSendGoogleIdToken(string idToken)
        {
            var body = new JsonObject {{"idToken", idToken}};
            var bodyString = body.ToJsonString(new JsonSerializerOptions {WriteIndented = true});
            var request =
                new UnityWebRequest(
                    "https://developer-mode.nine-chronicles.com/api/auth/login/google",
                    "POST");
            var jsonToSend = new System.Text.UTF8Encoding().GetBytes(bodyString);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 180;
            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("accept", "application/json");
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log(request.url);
            yield return request.SendWebRequest();
            var json = request.downloadHandler.text;
            Debug.Log(json);
        }

        public void OnSignInSilently()
        {
            GoogleSignIn.Configuration = configuration;
            GoogleSignIn.Configuration.UseGameSignIn = false;
            GoogleSignIn.Configuration.RequestIdToken = true;
            Debug.Log("Calling SignIn Silently");

            GoogleSignIn.DefaultInstance.SignInSilently()
                .ContinueWith(OnAuthenticationFinished);
        }
    }
}
