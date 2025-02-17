using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using UnityEngine;
using UnityEngine.Networking;
using Libplanet.Crypto;
using Nekoyume.UI;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using UniRx;

namespace Nekoyume.Game.OAuth
{
    public class AppleSigninBehaviour : MonoBehaviour
    {
        public enum SignInState
        {
            Undefined,
            Signed,
            Canceled,
            Waiting
        }

        private const string AppleUserIdKey = "AppleUserId";

        private IAppleAuthManager _appleAuthManager;

        public ReactiveProperty<SignInState> State { get; } = new(SignInState.Undefined);

        public string Email { get; private set; } = string.Empty;
        public string IdToken { get; private set; } = string.Empty;
        public Address? AgentAddress { get; private set; } = null;

        // Start is called before the first frame update
        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            // If the current platform is supported
            if (AppleAuthManager.IsCurrentPlatformSupported && _appleAuthManager == null)
            {
                // Creates a default JSON deserializer, to transform JSON Native responses to C# instances
                var deserializer = new PayloadDeserializer();
                // Creates an Apple Authentication manager with the deserializer
                _appleAuthManager = new AppleAuthManager(deserializer);

                _appleAuthManager.SetCredentialsRevokedCallback(result =>
                {
                    NcDebug.Log("Received revoked callback " + result);
                    PlayerPrefs.DeleteKey(AppleUserIdKey);
                });
            }
        }

        // Update is called once per frame
        private void Update()
        {
            // Updates the AppleAuthManager instance to execute
            // pending callbacks inside Unity's execution loop
            if (_appleAuthManager != null)
            {
                _appleAuthManager.Update();
            }
        }

        private void CheckCredentialStatusForUserId(string appleUserId)
        {
            // If there is an apple ID available, we should check the credential state
            _appleAuthManager.GetCredentialState(
                appleUserId,
                state =>
                {
                    switch (state)
                    {
                        // If it's authorized, login with that user id
                        case CredentialState.Authorized:
                            // this.SetupGameMenu(appleUserId, null);
                            State.Value = SignInState.Signed;
                            return;

                        // If it was revoked, or not found, we need a new sign in with apple attempt
                        // Discard previous apple user id
                        case CredentialState.Revoked:
                        case CredentialState.NotFound:
                            // this.SetupLoginMenuForSignInWithApple();
                            PlayerPrefs.DeleteKey(AppleUserIdKey);
                            State.Value = SignInState.Undefined;
                            return;
                    }
                },
                error =>
                {
                    var authorizationErrorCode = error.GetAuthorizationErrorCode();
                    NcDebug.LogWarning($"Error while trying to get credential state {authorizationErrorCode} {error} {error.Domain} {error.LocalizedDescription} {error.LocalizedFailureReason} {error.LocalizedRecoverySuggestion}");
                    // this.SetupLoginMenuForSignInWithApple();
                });
        }

        public void OnSignIn()
        {
            NcDebug.Log("[AppleSigninBehaviour] OnSignIn() invoked.");
            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);

            State.Value = SignInState.Waiting;
            _appleAuthManager.LoginWithAppleId(
                loginArgs,
                credential =>
                {
                    // If a sign in with apple succeeds, we should have obtained the credential with the user id, name, and email, save it
                    PlayerPrefs.SetString(AppleUserIdKey, credential.User);
                    Analyzer.Instance.Track("Unity/Intro/AppleSignIn/Signed");
                    var appleIdCredential = credential as IAppleIDCredential;
                    Email = appleIdCredential.Email;
                    IdToken = Encoding.UTF8.GetString(appleIdCredential.IdentityToken, 0, appleIdCredential.IdentityToken.Length);
                    State.Value = SignInState.Signed;
                },
                error =>
                {
                    var authorizationErrorCode = error.GetAuthorizationErrorCode();
                    NcDebug.LogWarning("Sign in with Apple failed " + authorizationErrorCode.ToString() + " " + error.ToString());
                    State.Value = SignInState.Canceled;
                });
            State.SkipLatestValueOnSubscribe().First().Subscribe(state =>
            {
                NcDebug.Log($"[AppleSigninBehaviour] State changed: {state}");
                switch (state)
                {
                    case SignInState.Signed:
                        Analyzer.Instance.Track("Unity/Intro/AppleSignIn/Signed");
                        break;
                    case SignInState.Canceled:
                        Analyzer.Instance.Track("Unity/Intro/AppleSignIn/Canceled");
                        break;
                }
            });
        }
    }
}
