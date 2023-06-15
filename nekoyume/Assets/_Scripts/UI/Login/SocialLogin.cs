using System.Threading.Tasks;
using Google;
using UnityEngine;

namespace Nekoyume.UI
{
    public class SocialLogin : MonoBehaviour
    {
        private string googleWebClientId;
        private GoogleSignInConfiguration googleSignInConfiguration;

        private void Start()
        {
            googleWebClientId = "<your client id here>";
            googleSignInConfiguration = new GoogleSignInConfiguration
            {
                WebClientId = googleWebClientId,
                UseGameSignIn = false,
                RequestIdToken = true,
                RequestEmail = true,
            };
        }

        public void Signin(System.Action onSuccess = null)
        {
            if (Platform.IsMobilePlatform())
            {
                GoogleSignIn.Configuration = googleSignInConfiguration;
                GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task =>
                {
                    OnAuthenticationFinished(task, onSuccess);
                });
            }
            else
            {
                onSuccess?.Invoke();
            }
        }

        private void OnAuthenticationFinished(
            Task<GoogleSignInUser> task,
            System.Action onSuccess = null)
        {
            if (task.IsFaulted)
            {
                using var enumerator = task.Exception!.InnerExceptions.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    var error = (GoogleSignIn.SignInException)enumerator.Current;
                    Debug.LogError($"Got Error: {error.Status} {error.Message}");
                }
                else
                {
                    Debug.LogError($"Got Unexpected Exception : {task.Exception}");
                }
            }
            else if (task.IsCanceled)
            {
                Debug.Log("Canceled");
            }
            else
            {
                Debug.Log($"Welcome: {task.Result.DisplayName}!");
                onSuccess?.Invoke();
            }
        }
    }
}
