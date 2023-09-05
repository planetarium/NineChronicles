using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Google;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Nekoyume.Game.OAuth
{
    public class GoogleSigninTest : MonoBehaviour
    {
        public TMP_Text statusText;

        private string webClientId = "340841465287-nuft8giaq0q2ci7utp4pi7fag191qc6s.apps.googleusercontent.com";

        private GoogleSignInConfiguration configuration;
        private string _idToken;

        // Defer the configuration creation until Awake so the web Client ID
        // Can be set via the property inspector in the Editor.
        private void Awake()
        {
            configuration = new GoogleSignInConfiguration
            {
                WebClientId = webClientId,
                RequestIdToken = true,
                RequestEmail = true,
                RequestAuthCode = true,
                RequestProfile = true,
            };
        }

        public void OnSignIn()
        {
            GoogleSignIn.Configuration = configuration;
            GoogleSignIn.Configuration.UseGameSignIn = false;
            GoogleSignIn.Configuration.RequestIdToken = true;
            AddStatusText("Calling SignIn");

            GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
                OnAuthenticationFinished);
        }

        public void OnSignOut()
        {
            AddStatusText("Calling SignOut");
            GoogleSignIn.DefaultInstance.SignOut();
        }

        public void OnDisconnect()
        {
            AddStatusText("Calling Disconnect");
            GoogleSignIn.DefaultInstance.Disconnect();
        }

        private void OnAuthenticationFinished(Task<GoogleSignInUser> task)
        {
            if (task.IsFaulted)
            {
                using (IEnumerator<System.Exception> enumerator =
                       task.Exception.InnerExceptions.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        GoogleSignIn.SignInException error =
                            (GoogleSignIn.SignInException) enumerator.Current;
                        AddStatusText("Got Error: " + error.Status + " " + error.Message);
                    }
                    else
                    {
                        AddStatusText("Got Unexpected Exception?!?" + task.Exception);
                    }
                }
            }
            else if (task.IsCanceled)
            {
                AddStatusText("Canceled");
            }
            else
            {
                var res = task.Result;
                AddStatusText("Welcome: " + res.UserId + "!" + $"\ntoken: {res.IdToken}");
                _idToken = res.IdToken;
            }
        }

        private void Update()
        {
            if (!string.IsNullOrEmpty(_idToken))
            {
                Debug.Log(_idToken);

                StartCoroutine(SendGoogleIdToken(_idToken));
                _idToken = string.Empty;
                AddStatusText("send id token");
            }
        }

        private IEnumerator SendGoogleIdToken(string idToken)
        {
            var formData = new List<IMultipartFormSection>
                {new MultipartFormDataSection("idToken", idToken)};

            var request = UnityWebRequest.Post(
                "https://developer-mode.nine-chronicles.com/api/auth/login/google", formData);
            request.timeout = 180;

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
            AddStatusText("Calling SignIn Silently");

            GoogleSignIn.DefaultInstance.SignInSilently()
                .ContinueWith(OnAuthenticationFinished);
        }


        public void OnGamesSignIn()
        {
            GoogleSignIn.Configuration = configuration;
            GoogleSignIn.Configuration.UseGameSignIn = true;
            GoogleSignIn.Configuration.RequestIdToken = false;

            AddStatusText("Calling Games SignIn");

            GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
                OnAuthenticationFinished);
        }

        private List<string> messages = new List<string>();

        void AddStatusText(string text)
        {
            if (messages.Count == 5)
            {
                messages.RemoveAt(0);
            }

            messages.Add(text);
            string txt = "";
            foreach (string s in messages)
            {
                txt += "\n" + s;
            }

            statusText.text = txt;
            Debug.Log(txt);
        }
    }
}
