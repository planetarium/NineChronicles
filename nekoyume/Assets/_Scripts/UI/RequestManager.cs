using System;
using System.Collections;
using Nekoyume.Pattern;
using UnityEngine.Networking;

namespace Nekoyume.UI
{
    public class RequestManager : MonoSingleton<RequestManager>
    {
        private int _isExistSeasonRewardRetryCount;
        private int _getSeasonRewardRetryCount;

        public IEnumerator GetJson(string url, Action<string> onSuccess, Action<UnityWebRequest> onFailed = null)
        {
            using var request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess(request.downloadHandler.text);
            }
            else
            {
                onFailed?.Invoke(request);
            }
        }

        public IEnumerator GetJson(
            string url,
            string headerName,
            string headerValue,
            Action<string> onSuccess,
            Action<UnityWebRequest> onFailed = null)
        {
            using var request = UnityWebRequest.Get(url);
            request.SetRequestHeader(headerName, headerValue);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess(request.downloadHandler.text);
            }
            else
            {
                onFailed?.Invoke(request);
            }
        }
    }
}
