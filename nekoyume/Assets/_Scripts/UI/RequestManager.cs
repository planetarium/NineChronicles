using System;
using System.Collections;
using Nekoyume.Pattern;
using UnityEngine.Networking;

namespace Nekoyume.UI
{
    public class RequestManager : MonoSingleton<RequestManager>
    {
        private const float RetryTime = 20f;
        private const float ShortRetryTime = 1f;
        private const int MaxRetryCount = 8;
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
    }
}
