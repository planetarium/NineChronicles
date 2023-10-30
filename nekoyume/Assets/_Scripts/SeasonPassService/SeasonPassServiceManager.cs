using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Linq;

namespace Nekoyume
{
    using UniRx;
    public class SeasonPassServiceManager : IDisposable
    {
        public int StageCourageAmount = 10;
        public int RankingCourageAmount = 10;
        public int WorldBossCourageAmount = 10;

        public SeasonPassServiceClient Client { get; private set; }

        public SeasonPassServiceClient.SeasonPassSchema CurrentSeasonPassData { get; private set; }
        public List<SeasonPassServiceClient.LevelInfoSchema> LevelInfos { get; private set; }
        public ReactiveProperty<SeasonPassServiceClient.UserSeasonPassSchema> AvatarInfo = new();

        public ReactiveProperty<DateTime> SeasonEndDate = new(DateTime.MinValue);
        public ReactiveProperty<string> RemainingDateTime = new("");

        public SeasonPassServiceManager(string url)
        {
            if(url == null)
            {
                Debug.LogError($"SeasonPassServiceManager Initialized Fail url is Null");
                return;
            }
            Client = new SeasonPassServiceClient(url);
            Initialize();
        }

        public void Initialize() {
            Client.GetSeasonpassCurrentAsync((result) =>
            {
                CurrentSeasonPassData = result;
                DateTime.TryParse(CurrentSeasonPassData.EndDate, out var endDateTime);
                SeasonEndDate.SetValueAndForceNotify(endDateTime);
                RefreshRemainingTime();
            }, (error) =>
            {
                Debug.LogError($"SeasonPassServiceManager Initialized Fail [GetSeasonpassCurrentAsync] error: {error}");
            }).AsUniTask().Forget();

            Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1)).Subscribe((time) =>
            {
                RefreshRemainingTime();
            }).AddTo(Game.Game.instance);

            Client.GetSeasonpassLevelAsync((result) =>
            {
                LevelInfos = result.OrderBy(info => info.Level).ToList();
            }, (error) =>
            {
                Debug.LogError($"SeasonPassServiceManager Initialized Fail [GetSeasonpassLevelAsync] error: {error}");
            }).AsUniTask().Forget();
        }

        private void RefreshRemainingTime()
        {
            var timeSpan = SeasonEndDate.Value - DateTime.Now;
            var dayExist = timeSpan.TotalDays > 1;
            var hourExist = timeSpan.TotalHours >= 1;
            var dayText = dayExist ? $"{(int)timeSpan.TotalDays}d " : string.Empty;
            var hourText = hourExist ? $"{(int)timeSpan.Hours}h " : string.Empty;
            RemainingDateTime.SetValueAndForceNotify($"{dayText}{hourText}");
        }

        public async Task AvatarStateRefresh()
        {
            if(CurrentSeasonPassData == null || LevelInfos == null) {
                return;
            }
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            await Client.GetUserStatusAsync(CurrentSeasonPassData.Id, avatarAddress.ToString(),
                (result) =>
                {
                    AvatarInfo.SetValueAndForceNotify(result);
                },
                (error) =>
                {
                    Debug.LogError($"SeasonPassServiceManager [AvatarStateRefresh] error: {error}");
                });   
        }

        public void ReceiveAll(Action<SeasonPassServiceClient.ClaimResultSchema> onSucces, Action<string> onError)
        {
            var agentAddress = Game.Game.instance.States.AgentState.address;
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            Client.PostUserClaimAsync(new SeasonPassServiceClient.ClaimRequestSchema
            {
                AgentAddr = agentAddress.ToString(),
                AvatarAddr = avatarAddress.ToString(),
                SeasonId = AvatarInfo.Value.SeasonPassId
            },
                (result) =>
                {
                    onSucces?.Invoke(result);
                },
                (error) =>
                {
                    Debug.LogError($"SeasonPassServiceManager [ReceiveAll] error: {error}");
                    onError?.Invoke(error);
                }).AsUniTask().Forget();
        }

        public void GetExp(int level,  out int minExp, out int maxExp)
        {
            if(LevelInfos == null)
            {
                Debug.LogError("[SeasonPassServiceManager] LevelInfos Not Setted");
                minExp = 0;
                maxExp = 0;
                return;
            }

            if(level > LevelInfos.Count)
            {
                minExp = LevelInfos[LevelInfos.Count - 2].Exp;
                maxExp = LevelInfos[LevelInfos.Count - 1].Exp;
                return;
            }

            minExp = level - 1 >= 0 ? LevelInfos[level - 1].Exp : 0;
            maxExp = LevelInfos[level].Exp;
            return;
        }

        public void Dispose()
        {
            Client?.Dispose();
        }
    }
}
