using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libplanet;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Pattern;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module.WorldBoss;
using Nekoyume.UI.Scroller;
using UnityEngine;
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

        public IEnumerator GetJson(string url, System.Action<string> onSuccess)
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
