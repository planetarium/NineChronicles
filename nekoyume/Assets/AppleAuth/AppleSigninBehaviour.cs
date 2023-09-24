using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
using AppleAuth.Native;

namespace Nekoyume.Game.OAuth
{
    public class AppleSigninBehaviour : MonoBehaviour
    {
        private IAppleAuthManager _appleAuthManager;

        // Start is called before the first frame update
        private void Start()
        {
            // If the current platform is supported
            if (AppleAuthManager.IsCurrentPlatformSupported)
            {
                // Creates a default JSON deserializer, to transform JSON Native responses to C# instances
                var deserializer = new PayloadDeserializer();
                // Creates an Apple Authentication manager with the deserializer
                this._appleAuthManager = new AppleAuthManager(deserializer);    
            }
        }

        // Update is called once per frame
        private void Update()
        {
            // Updates the AppleAuthManager instance to execute
            // pending callbacks inside Unity's execution loop
            if (this._appleAuthManager != null)
            {
                this._appleAuthManager.Update();
            }
        }
        
        public void SignInWithApple()
        {
            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);
            
            this._appleAuthManager.LoginWithAppleId(
                loginArgs,
                credential =>
                {
                    // If a sign in with apple succeeds, we should have obtained the credential with the user id, name, and email, save it
                    // PlayerPrefs.SetString(AppleUserIdKey, credential.User);
                    // this.SetupGameMenu(credential.User, credential);
                },
                error =>
                {
                    var authorizationErrorCode = error.GetAuthorizationErrorCode();
                    Debug.LogWarning("Sign in with Apple failed " + authorizationErrorCode.ToString() + " " + error.ToString());
                    // this.SetupLoginMenuForSignInWithApple();
                });
        }
    }
}
