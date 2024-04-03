using System.Threading.Tasks;
using Google;
using Libplanet.Crypto;
using UniRx;
using UnityEngine;

namespace Nekoyume.Game.OAuth
{
    public class GoogleSigninBehaviour : MonoBehaviour
    {
        public enum SignInState
        {
            Undefined,
            Signed,
            Canceled,
            Waiting
        }

        public ReactiveProperty<SignInState> State { get; } = new(SignInState.Undefined);

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

        public string Email { get; private set; } = string.Empty;
        public string IdToken { get; private set; } = string.Empty;

        public void OnSignIn()
        {
            NcDebugger.Log("[GoogleSigninBehaviour] OnSignIn() invoked.");
            GoogleSignIn.Configuration = _configuration;
            State.Value = SignInState.Waiting;
            GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnAuthenticationFinished);
            State.SkipLatestValueOnSubscribe().First().Subscribe(state =>
            {
                NcDebugger.Log($"[GoogleSigninBehaviour] State changed: {state}");
                switch (state)
                {
                    case SignInState.Signed:
                        Analyzer.Instance.Track("Unity/Intro/GoogleSignIn/Signed");

                        var signedEvt = new AirbridgeEvent("Intro_GoogleSignIn_Signed");
                        AirbridgeUnity.TrackEvent(signedEvt);
                        break;
                    case SignInState.Canceled:
                        Analyzer.Instance.Track("Unity/Intro/GoogleSignIn/Canceled");

                        var canceledEvt = new AirbridgeEvent("Intro_GoogleSignIn_Canceled");
                        AirbridgeUnity.TrackEvent(canceledEvt);
                        break;
                }
            });
        }

        public void OnSignInSilently()
        {
            NcDebugger.Log("[GoogleSigninBehaviour] OnSignInSilently() invoked.");
            GoogleSignIn.Configuration = _configuration;
            GoogleSignIn.DefaultInstance.SignInSilently()
                .ContinueWith(OnAuthenticationFinished);
        }

        public void OnSignOut()
        {
            NcDebugger.Log("[GoogleSigninBehaviour] OnSignOut() invoked.");
            GoogleSignIn.DefaultInstance.SignOut();
        }

        public void OnDisconnect()
        {
            NcDebugger.Log("[GoogleSigninBehaviour] OnDisconnect() invoked.");
            GoogleSignIn.DefaultInstance.Disconnect();
        }

        private void OnAuthenticationFinished(Task<GoogleSignInUser> task)
        {
            NcDebugger.Log("[GoogleSigninBehaviour] OnAuthenticationFinished() invoked.");
            if (task.IsFaulted)
            {
                NcDebugger.LogWarning("[GoogleSigninBehaviour] OnAuthenticationFinished()..." +
                               " task is faulted.");
                using var enumerator =
                    task.Exception?.InnerExceptions.GetEnumerator();
                if (enumerator != null &&
                    enumerator.MoveNext() &&
                    enumerator.Current is GoogleSignIn.SignInException e)
                {
                    NcDebugger.LogException(e);
                }
                else
                {
                    NcDebugger.LogError("[GoogleSigninBehaviour] OnAuthenticationFinished()..." +
                                   " unexpected exception occurred.");
                    NcDebugger.LogException(task.Exception);
                }

                State.Value = SignInState.Canceled;
            }
            else if (task.IsCanceled)
            {
                NcDebugger.Log("[GoogleSigninBehaviour] OnAuthenticationFinished()... task is canceled.");
                State.Value = SignInState.Canceled;
            }
            else
            {
                var res = task.Result;
                NcDebugger.Log("[GoogleSigninBehaviour] OnAuthenticationFinished()..." +
                          $" Welcome!! {res.Email}, userId({res.UserId}), token({res.IdToken})");
                Email = res.Email;
                IdToken = res.IdToken;
                State.Value = SignInState.Signed;
            }
        }
    }
}
