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

        public void IsExistSeasonReward(int raidId, Address avatarAddress)
        {
            _isExistSeasonRewardRetryCount = 0;
            RequestIsExistSeasonReward(raidId, avatarAddress);
        }

        public async void GetSeasonReward(string json)
        {
            var result = JsonUtility.FromJson<SeasonRewardRecord>(json);
            var rewards = result.rewards.ToList();
            _getSeasonRewardRetryCount = 0;
            await RequestGetSeasonReward(rewards);
        }

        private void RequestIsExistSeasonReward(int raidId, Address avatarAddress)
        {
            if (_isExistSeasonRewardRetryCount > MaxRetryCount)
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_BOSS_SEASON_REWARD_REQUEST_FAIL"),
                    NotificationCell.NotificationType.Alert);
                return;
            }

            _isExistSeasonRewardRetryCount++;
            StartCoroutine(WorldBossQuery.CoIsExistSeasonReward(raidId, avatarAddress,
                onSuccess: (b) =>
                {
                    WorldBossStates.SetHasSeasonRewards(avatarAddress, b.Contains("true"));
                },
                onFailed: (id, address) =>
                {
                    StartCoroutine(CoRequestIsExistSeasonReward(id, address));
                }));
        }

        private IEnumerator CoRequestIsExistSeasonReward(int raidId, Address avatarAddress)
        {
            yield return new WaitForSeconds(ShortRetryTime);
            RequestIsExistSeasonReward(raidId, avatarAddress);
        }

        private async Task RequestGetSeasonReward(List<SeasonRewards> rewards)
        {
            var received = await WorldBossQuery.CheckTxStatus(rewards);
            if (received.Count == rewards.Count)
            {
                var avatarAddress = States.Instance.CurrentAvatarState.address;
                var agentAddress = States.Instance.AgentState.address;
                var crystal = await Game.Game.instance.Agent.GetBalanceAsync(agentAddress, CrystalCalculator.CRYSTAL);
                States.Instance.SetCrystalBalance(crystal);
                await WorldBossStates.Set(avatarAddress);
                WorldBossStates.SetReceivingSeasonRewards(avatarAddress, false);
                WorldBossStates.SetHasSeasonRewards(avatarAddress, false);
                Widget.Find<WorldBossRewardScreen>().Show(received);
            }
            else
            {
                StartCoroutine(CoCheckTxAgain(rewards));
            }
        }

        private IEnumerator CoCheckTxAgain(List<SeasonRewards> rewards)
        {
            if (_getSeasonRewardRetryCount > MaxRetryCount)
            {
                OneLineSystem.Push(
                    MailType.System,
                    L10nManager.Localize("UI_BOSS_SEASON_REWARD_TX_FAIL"),
                    NotificationCell.NotificationType.Alert);
                yield break;
            }

            yield return new WaitForSeconds(RetryTime);
            _getSeasonRewardRetryCount++;
            RequestGetSeasonReward(rewards);
        }
    }
}
