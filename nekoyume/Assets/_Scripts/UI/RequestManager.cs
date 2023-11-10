using System;
using System.Collections;
using Nekoyume.Pattern;
using UnityEngine;
using UnityEngine.Networking;

namespace Nekoyume.UI
{
    public class RequestManager : MonoSingleton<RequestManager>
    {
        private const int Timeout = 30;

        private int _isExistSeasonRewardRetryCount;
        private int _getSeasonRewardRetryCount;

        public IEnumerator GetJson(
            string url,
            Action<string> onSuccess,
            Action<UnityWebRequest> onFailed = null)
        {
            Debug.Log($"[RequestManager] GetJson: {url}");
            using var request = MakeRequestWithTimeout(url);
            request.SetRequestHeader("Cache-Control", "no-cache");
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[RequestManager] GetJson Success: {url}, {request.downloadHandler.text}");
                onSuccess(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"[RequestManager] GetJson Failed: {url}, {request.error}");
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
            Debug.Log($"[RequestManager] GetJson: {url}, {headerName}, {headerValue}");
            using var request = MakeRequestWithTimeout(url);
            request.SetRequestHeader(headerName, headerValue);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[RequestManager] GetJson Success: {url}, {request.downloadHandler.text}");
                onSuccess(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"[RequestManager] GetJson Failed: {url}, {request.error}");
                onFailed?.Invoke(request);
            }
        }

        private static UnityWebRequest MakeRequestWithTimeout(string url)
        {
            var request = UnityWebRequest.Get(url);
            request.timeout = Timeout;
            return request;
        }
    }
}
