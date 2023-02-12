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

        public IEnumerator GetJson(string url, Action<string> onSuccess)
        {
            using var request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess(request.downloadHandler.text);
            }
        }

        public IEnumerator Post(string url, string json, Action<string> onSuccess)
        {
            using var request = UnityWebRequest.Post(url, json);
            var jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess(request.downloadHandler.text);
            }
        }
    }
}
