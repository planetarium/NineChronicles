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
                DateTime.TryParse(CurrentSeasonPassData.EndDate, out var test);
                SeasonEndDate.Value = test;
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
                    IsPremium.Value = AvatarInfo.IsPremium;
                    AvatarExp.Value = AvatarInfo.Exp;
                    SeasonPassLevel.Value = AvatarInfo.Level;
                },
                (error) =>
                {
                    Debug.LogError($"SeasonPassServiceManager [AvatarStateRefresh] error: {error}");
                });
            
        }

        public void Dispose()
        {
            Client?.Dispose();
        }
    }
}
