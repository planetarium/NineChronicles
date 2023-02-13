using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Pattern;
using Nekoyume.UI.Model;
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
    }
}
