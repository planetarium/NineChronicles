using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

namespace Nekoyume
{
    public class SeasonPassServiceManager : IDisposable
    {
        public SeasonPassServiceClient Client { get; private set; }

        public SeasonPassServiceClient.SeasonPassSchema CurrentSeasonPassData { get; private set; }
        public SeasonPassServiceClient.LevelInfoSchema[] LevelInfos { get; private set; }
        public SeasonPassServiceClient.UserSeasonPassSchema AvatarInfo { get; private set; }

        public ReactiveProperty<bool> IsPremium = new(false);
        public ReactiveProperty<int> SeasonPassLevel = new (0);
        public ReactiveProperty<int> AvatarExp = new (0);

        public ReactiveProperty<DateTime> SeasonEndDate = new(DateTime.MinValue);

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
            }, (error) =>
            {
                Debug.LogError($"SeasonPassServiceManager Initialized Fail [GetSeasonpassCurrentAsync] error: {error}");
            }).AsUniTask().Forget();
            Client.GetSeasonpassLevelAsync((result) =>
            {
                LevelInfos = result;
            }, (error) =>
            {
                Debug.LogError($"SeasonPassServiceManager Initialized Fail [GetSeasonpassLevelAsync] error: {error}");
            }).AsUniTask().Forget();
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
                    AvatarInfo = result;
                    IsPremium.SetValueAndForceNotify(AvatarInfo.IsPremium);
                    AvatarExp.SetValueAndForceNotify(AvatarInfo.Exp);
                    SeasonPassLevel.SetValueAndForceNotify(AvatarInfo.Level);
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
                SeasonId = AvatarInfo.SeasonPassId
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

        public void Dispose()
        {
            Client?.Dispose();
        }
    }
}
