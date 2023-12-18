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
        public int AdventureCourageAmount = 10;
        public int AdventureSweepCourageAmount = 10;
        public int ArenaCourageAmount = 10;
        public int WorldBossCourageAmount = 10;

        public SeasonPassServiceClient Client { get; private set; }

        public SeasonPassServiceClient.SeasonPassSchema CurrentSeasonPassData { get; private set; }
        public List<SeasonPassServiceClient.LevelInfoSchema> LevelInfos { get; private set; }
        public ReactiveProperty<SeasonPassServiceClient.UserSeasonPassSchema> AvatarInfo = new();

        public ReactiveProperty<DateTime> SeasonEndDate = new(DateTime.MinValue);
        public ReactiveProperty<string> RemainingDateTime = new("");

        public string GoogleMarketURL = "https://play.google.com/store/search?q=Nine%20Chronicles&c=apps&hl=en-EN";// default
        public string AppleMarketURL = "https://nine-chronicles.com/";// default

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

        public void Initialize()
        {
            Client.GetSeasonpassCurrentAsync((result) =>
            {
                CurrentSeasonPassData = result;
                DateTime.TryParse(CurrentSeasonPassData.EndTimestamp, out var endDateTime);
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

            RefreshSeassonpassExpAmount();

            Game.Event.OnRoomEnter.AddListener(_ => AvatarStateRefreshAsync().AsUniTask().Forget());
        }

        private void RefreshSeassonpassExpAmount()
        {
            Client.GetSeasonpassLevelAsync((result) =>
            {
                LevelInfos = result.OrderBy(info => info.Level).ToList();
            }, (error) =>
            {
                Debug.LogError($"SeasonPassServiceManager RefreshSeassonpassExpAmount [GetSeasonpassLevelAsync] error: {error}");
            }).AsUniTask().Forget();

            Client.GetSeasonpassExpAsync((result) =>
            {
                foreach (var item in result)
                {
                    switch (item.ActionType)
                    {
                        case SeasonPassServiceClient.ActionType.hack_and_slash:
                            AdventureCourageAmount = item.Exp;
                            break;
                        case SeasonPassServiceClient.ActionType.hack_and_slash_sweep:
                            AdventureSweepCourageAmount = item.Exp;
                            break;
                        case SeasonPassServiceClient.ActionType.battle_arena:
                            ArenaCourageAmount = item.Exp;
                            break;
                        case SeasonPassServiceClient.ActionType.raid:
                            WorldBossCourageAmount = item.Exp;
                            break;
                        default:
                            break;
                    }
                }
            }, (error) =>
            {
                Debug.LogError($"SeasonPassServiceManager RefreshSeassonpassExpAmount [GetSeasonpassExpAsync] error: {error}");
            }).AsUniTask().Forget();
        }

        private void RefreshRemainingTime()
        {
            var timeSpan = SeasonEndDate.Value - DateTime.Now;
            var dayExist = timeSpan.TotalDays > 1;
            var hourExist = timeSpan.TotalHours >= 1;
            var dayText = dayExist ? $"{(int)timeSpan.TotalDays}d " : string.Empty;
            var hourText = hourExist ? $"{(int)timeSpan.Hours}h " : string.Empty;
            var minText = string.Empty;
            if(!hourExist && !dayExist)
            {
                minText = $"{(int)timeSpan.Minutes}m";
            }
            RemainingDateTime.SetValueAndForceNotify($"{dayText}{hourText}{minText}");
        }

        public async Task AvatarStateRefreshAsync()
        {
            if(CurrentSeasonPassData == null || LevelInfos == null) {
                AvatarInfo.SetValueAndForceNotify(null);
                Debug.LogError("$SeasonPassServiceManager [AvatarStateRefreshAsync] CurrentSeasonPassData or LevelInfos is null");
                return;
            }

            if (!Game.Game.instance.CurrentPlanetId.HasValue)
            {
                AvatarInfo.SetValueAndForceNotify(null);
                Debug.LogError("$SeasonPassServiceManager [AvatarStateRefreshAsync] Game.Game.instance.CurrentPlanetId is null");
                return;
            }

            if(Game.Game.instance.States == null || Game.Game.instance.States.CurrentAvatarState == null)
            {
                AvatarInfo.SetValueAndForceNotify(null);
                Debug.LogError("$SeasonPassServiceManager [AvatarStateRefreshAsync] States or CurrentAvatarState is null");
                return;
            }

            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;

            if(Client == null)
            {
                AvatarInfo.SetValueAndForceNotify(null);
                Debug.LogError("$SeasonPassServiceManager [AvatarStateRefreshAsync] Client is null");
                return;
            }

            await Client.GetSeasonpassCurrentAsync(
                (result) =>
                {
                    if (CurrentSeasonPassData.Id == result.Id)
                        return;

                    CurrentSeasonPassData = result;
                    DateTime.TryParse(CurrentSeasonPassData.EndTimestamp, out var endDateTime);
                    SeasonEndDate.SetValueAndForceNotify(endDateTime);
                    RefreshRemainingTime();
                    RefreshSeassonpassExpAmount();
                },
                (error) =>
                {
                    Debug.LogError($"SeasonPassServiceManager [AvatarStateRefresh] [GetSeasonpassCurrentAsync] error: {error}");
                });
            
            await Client.GetUserStatusAsync(CurrentSeasonPassData.Id, avatarAddress.ToString(), Game.Game.instance.CurrentPlanetId.ToString(),
                (result) =>
                {
                    AvatarInfo.SetValueAndForceNotify(result);
                },
                (error) =>
                {
                    AvatarInfo.SetValueAndForceNotify(null);
                    Debug.LogError($"SeasonPassServiceManager [AvatarStateRefresh] error: {error}");
                });   
        }

        public void ReceiveAll(Action<SeasonPassServiceClient.ClaimResultSchema> onSucces, Action<string> onError)
        {
            var agentAddress = Game.Game.instance.States.AgentState.address;
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            if (!Game.Game.instance.CurrentPlanetId.HasValue)
            {
                var errorString = "$SeasonPassServiceManager [ReceiveAll] Game.Game.instance.CurrentPlanetId is null";
                Debug.LogError(errorString);
                onError?.Invoke(errorString);
                return;
            }
            Client.PostUserClaimAsync(new SeasonPassServiceClient.ClaimRequestSchema
            {
                AgentAddr = agentAddress.ToString(),
                AvatarAddr = avatarAddress.ToString(),
                SeasonId = AvatarInfo.Value.SeasonPassId,
                PlanetId = Enum.Parse<SeasonPassServiceClient.PlanetID>($"_{Game.Game.instance.CurrentPlanetId}")
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

            if(level >= LevelInfos.Count)
            {
                minExp = LevelInfos[LevelInfos.Count - 2].Exp;
                maxExp = LevelInfos[LevelInfos.Count - 1].Exp;
                return;
            }

            minExp = level - 1 >= 0 ? LevelInfos[level - 1].Exp : 0;
            maxExp = LevelInfos[level].Exp;
            return;
        }

        public string GetSeassonPassPopupViewKey()
        {
            var result = "SeasonPassCouragePopupViewd" + Game.Game.instance.States.CurrentAvatarState.address.ToHex() + "SeassonPassID" + CurrentSeasonPassData.Id;
            return result;
        }

        public void Dispose()
        {
            Client?.Dispose();
        }
    }
}
