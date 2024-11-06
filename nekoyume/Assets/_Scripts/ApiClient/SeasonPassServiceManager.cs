using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Linq;

namespace Nekoyume.ApiClient
{
    using UniRx;
    using UnityEngine;

    public class SeasonPassServiceManager : IDisposable
    {
        public int AdventureCourageAmount = 10;
        public int AdventureSweepCourageAmount = 10;
        public int ArenaCourageAmount = 10;
        public int WorldBossCourageAmount = 10;
        public int EventDungeonCourageAmount = 10;

        public SeasonPassServiceClient Client { get; private set; }

        public Dictionary<SeasonPassServiceClient.PassType, SeasonPassServiceClient.SeasonPassSchema> CurrentSeasonPassData { get; private set; } = new Dictionary<SeasonPassServiceClient.PassType, SeasonPassServiceClient.SeasonPassSchema>();
        public Dictionary<SeasonPassServiceClient.PassType, List<SeasonPassServiceClient.LevelInfoSchema>> LevelInfos { get; private set; } = new Dictionary<SeasonPassServiceClient.PassType, List<SeasonPassServiceClient.LevelInfoSchema>>();
        public Dictionary<SeasonPassServiceClient.PassType, SeasonPassServiceClient.UserSeasonPassSchema> AvatarInfo = new();

        //Courage Season pass의 주기를 공통으로 사용함으로 해당데이터는 분기처리해두지않음.
        public ReactiveProperty<DateTime> SeasonEndDate = new(DateTime.MinValue);
        public ReactiveProperty<string> RemainingDateTime = new("");

        public Dictionary<SeasonPassServiceClient.PassType, DateTime> PrevSeasonClaimEndDates = new Dictionary<SeasonPassServiceClient.PassType, DateTime>();

        public string GoogleMarketURL { get; set; } = "https://play.google.com/store/search?q=Nine%20Chronicles&c=apps&hl=en-EN"; // default
        public string AppleMarketURL { get; set; } = "https://nine-chronicles.com/"; // default

        public bool IsInitialized => Client != null;

        private SeasonPassServiceClient.PassType[] passTypes =
        {
            SeasonPassServiceClient.PassType.CouragePass,
            SeasonPassServiceClient.PassType.WorldClearPass,
            SeasonPassServiceClient.PassType.AdventureBossPass
        };

        public HashSet<SeasonPassServiceClient.PassType> HasClaimPassType { get; private set; } = new HashSet<SeasonPassServiceClient.PassType>();
        public HashSet<SeasonPassServiceClient.PassType> HasPrevClaimPassType { get; private set; } = new HashSet<SeasonPassServiceClient.PassType>();


        public SeasonPassServiceManager(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                NcDebug.LogError($"SeasonPassServiceManager Initialized Fail url is Null");
                return;
            }
            Client = new SeasonPassServiceClient(url);
            Initialize();
        }

        private async Task FetchCurrentSeasonPassDatas()
        {
            // Fetch current season pass data with retries
            foreach (var passType in passTypes)
            {
                if (!IsCurrentSeasonPassValid(passType))
                {
                    await FetchSeasonPassDataWithRetry(passType);
                }
            }

            // Fetch level info data with retries
            foreach (var passType in passTypes)
            {
                await FetchLevelInfoDataWithRetry(passType);
            }
        }

        private bool IsCurrentSeasonPassValid(SeasonPassServiceClient.PassType passType)
        {
            if (CurrentSeasonPassData != null &&
                CurrentSeasonPassData.TryGetValue(passType, out var courageSeasonPassSchema) &&
                DateTime.TryParse(courageSeasonPassSchema.EndTimestamp, out var endDateTime) &&
                endDateTime >= DateTime.Now)
            {
                return true;
            }
            return false;
        }

        private async Task FetchSeasonPassDataWithRetry(SeasonPassServiceClient.PassType passType, int maxRetries = 3)
        {
            int retryCount = 0;
            while (retryCount < maxRetries)
            {
                var tcs = new TaskCompletionSource<bool>();

                await Client.GetSeasonpassCurrentAsync(passType, (result) =>
                {
                    CurrentSeasonPassData[passType] = result;

                    // 용기 시즌패스의 경우만 종료일을 설정하고 남은 시간을 갱신한다.
                    if (passType == SeasonPassServiceClient.PassType.CouragePass)
                    {
                        DateTime.TryParse(result.EndTimestamp, out var endDateTime);
                        SeasonEndDate.SetValueAndForceNotify(endDateTime);
                        RefreshRemainingTime();
                    }

                    tcs.SetResult(true);
                },
                (error) =>
                {
                    NcDebug.LogError($"SeasonPassServiceManager Initialized Fail [GetSeasonPassCurrentAsync] [{passType}] error: {error}");
                    tcs.SetResult(false);
                });

                if (await tcs.Task)
                    break;

                retryCount++;
            }
        }

        private async Task FetchLevelInfoDataWithRetry(SeasonPassServiceClient.PassType passType, int maxRetries = 3)
        {
            int retryCount = 0;
            while (retryCount < maxRetries)
            {
                var tcs = new TaskCompletionSource<bool>();

                await Client.GetSeasonpassLevelAsync(passType, (result) =>
                {
                    LevelInfos[passType] = result.OrderBy(info => info.Level).ToList();
                    tcs.SetResult(true);
                },
                (error) =>
                {
                    NcDebug.LogError($"SeasonPassServiceManager RefreshSeasonpassExpAmount [GetSeasonpassLevelAsync] [{passType}] error: {error}");
                    tcs.SetResult(false);
                });

                if (await tcs.Task)
                    break;

                retryCount++;
            }
        }

        private void Initialize()
        {
            Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1)).Subscribe((time) =>
            {
                RefreshRemainingTime();
            }).AddTo(Game.Game.instance);

            RefreshSeasonPassExpAmount();

            Game.Lobby.OnLobbyEnterEvent += () => AvatarStateRefreshAsync().AsUniTask().Forget();
        }

        private void RefreshSeasonPassExpAmount()
        {
            void GetSeasonPassExpRetry(int retry)
            {
                Client.GetSeasonpassExpAsync(SeasonPassServiceClient.PassType.CouragePass, (result) =>
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
                            case SeasonPassServiceClient.ActionType.event_dungeon:
                                EventDungeonCourageAmount = item.Exp;
                                break;
                            default:
                                break;
                        }
                    }
                }, (error) =>
                {
                    NcDebug.LogError($"SeasonPassServiceManager RefreshSeassonpassExpAmount [GetSeasonpassExpAsync] error: {error}");
                    if (retry <= 0)
                    {
                        return;
                    }

                    GetSeasonPassExpRetry(--retry);
                }).AsUniTask().Forget();
            }

            GetSeasonPassExpRetry(3);
        }

        private void RefreshRemainingTime()
        {
            var timeSpan = SeasonEndDate.Value - DateTime.Now;
            var dayExist = timeSpan.TotalDays > 1;
            var hourExist = timeSpan.TotalHours >= 1;
            var dayText = dayExist ? $"{(int)timeSpan.TotalDays}d " : string.Empty;
            var hourText = hourExist ? $"{(int)timeSpan.Hours}h " : string.Empty;
            var minText = string.Empty;
            if (!hourExist && !dayExist)
            {
                minText = $"{(int)timeSpan.Minutes}m";
            }

            RemainingDateTime.SetValueAndForceNotify($"{dayText}{hourText}{minText}");
        }

        public string GetPrevRemainingClaim(SeasonPassServiceClient.PassType passType)
        {
            if (!PrevSeasonClaimEndDates.TryGetValue(passType, out var claimEndDate)|| claimEndDate < DateTime.Now || !HasPrevClaimPassType.Contains(passType))
            {
                return "0m";
            }

            var prevSeasonTimeSpan = claimEndDate - DateTime.Now;
            var prevSeasonDayExist = prevSeasonTimeSpan.TotalDays > 1;
            var prevSeasonHourExist = prevSeasonTimeSpan.TotalHours >= 1;
            var prevSeasonDayText = prevSeasonDayExist ? $"{(int)prevSeasonTimeSpan.TotalDays}d " : string.Empty;
            var prevSeasonHourText = prevSeasonHourExist ? $"{(int)prevSeasonTimeSpan.Hours}h " : string.Empty;
            var prevSeasonMinText = string.Empty;
            if (!prevSeasonHourExist && !prevSeasonDayExist)
            {
                prevSeasonMinText = $"{(int)prevSeasonTimeSpan.Minutes}m";
            }

            return $"{prevSeasonDayText}{prevSeasonHourText}{prevSeasonMinText}";
        }

        public async Task AvatarStateRefreshAsync()
        {
            if (Client == null)
            {
                NcDebug.LogWarning("$SeasonPassServiceManager [AvatarStateRefreshAsync] Client is null");
                return;
            }

            if (CurrentSeasonPassData == null || LevelInfos == null)
            {
                NcDebug.LogError("$SeasonPassServiceManager [AvatarStateRefreshAsync] CurrentSeasonPassData or LevelInfos is null");
                return;
            }

            Game.Game.instance.CurrentPlanetId = Nekoyume.Multiplanetary.PlanetId.OdinInternal;

            if (!Game.Game.instance.CurrentPlanetId.HasValue)
            {
                NcDebug.LogError("$SeasonPassServiceManager [AvatarStateRefreshAsync] Game.Game.instance.CurrentPlanetId is null");
                return;
            }

            if (Game.Game.instance.States == null || Game.Game.instance.States.CurrentAvatarState == null)
            {
                NcDebug.LogError("$SeasonPassServiceManager [AvatarStateRefreshAsync] States or CurrentAvatarState is null");
                return;
            }

            await FetchCurrentSeasonPassDatas();
            await FetchUserAllStatus();
        }

        public async Task FetchUserAllStatus()
        {
            HasClaimPassType.Clear();
            HasPrevClaimPassType.Clear();
            foreach (var passType in passTypes)
            {
                await FetchUserStatus(passType, CurrentSeasonPassData[passType].SeasonIndex);
            }
        }

        private async Task FetchUserStatus(SeasonPassServiceClient.PassType passType, int seasonIndex)
        {
            await Client.GetUserStatusAsync(
                        Game.Game.instance.CurrentPlanetId.ToString(),
                        Game.Game.instance.States.CurrentAvatarState.address.ToString(),
                        passType,
                        seasonIndex, (result) =>
                        {
                            AvatarInfo[passType] = result;
                            if(result.Level > result.LastNormalClaim || (result.IsPremium && result.Level > result.LastPremiumClaim))
                            {
                                HasClaimPassType.Add(passType);
                            }
                        },
                        (error) =>
                        {
                            NcDebug.LogError($"SeasonPassServiceManager [FetchUserStatus] error: {error}");
                        });

            //이전 시즌정보 조회
            await Client.GetUserStatusAsync(
                        Game.Game.instance.CurrentPlanetId.ToString(),
                        Game.Game.instance.States.CurrentAvatarState.address.ToString(),
                        passType,
                        seasonIndex - 1, (result) =>
                        {
                            DateTime.TryParse(result.ClaimLimitTimestamp, out var claimLimitTimestamp);
                            PrevSeasonClaimEndDates[passType] = claimLimitTimestamp;

                            //이전시즌 보상의경우 프리미엄일때만 체크합니다.
                            //https://github.com/planetarium/NineChronicles/issues/4731#issuecomment-2044277184
                            if (result.IsPremium && result.Level > result.LastPremiumClaim)
                            {
                                HasPrevClaimPassType.Add(passType);
                            }
                        },
                        (error) =>
                        {

                        });
        }

        public void ReceiveAll(SeasonPassServiceClient.PassType passType, Action<SeasonPassServiceClient.ClaimResultSchema> onSucces, Action<string> onError)
        {
            var agentAddress = Game.Game.instance.States.AgentState.address;
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            if (!Game.Game.instance.CurrentPlanetId.HasValue)
            {
                var errorString = "$SeasonPassServiceManager [ReceiveAll] Game.Game.instance.CurrentPlanetId is null";
                NcDebug.LogError(errorString);
                onError?.Invoke(errorString);
                return;
            }

            if (!CurrentSeasonPassData.TryGetValue(passType, out var seasonPassSchema))
            {
                var errorString = $"SeasonPassServiceManager [PrevClaim] CurrentSeasonPassData {passType} is null";
                NcDebug.LogError(errorString);
                onError?.Invoke(errorString);
                return;
            }

            Client.PostUserClaimAsync(new SeasonPassServiceClient.ClaimRequestSchema
            {
                AgentAddr = agentAddress.ToString(),
                AvatarAddr = avatarAddress.ToString(),
                SeasonIndex = seasonPassSchema.SeasonIndex,
                PassType = passType,
                PlanetId = Enum.Parse<SeasonPassServiceClient.PlanetID>($"_{Game.Game.instance.CurrentPlanetId}"),
                Force = false,
                Prev = false
            },
                (result) => { onSucces?.Invoke(result); },
                (error) =>
                {
                    NcDebug.LogError($"SeasonPassServiceManager [ReceiveAll] error: {error}");
                    onError?.Invoke(error);
                }).AsUniTask().Forget();
        }

        public void PrevClaim(SeasonPassServiceClient.PassType passType, Action<SeasonPassServiceClient.ClaimResultSchema> onSuccess, Action<string> onError)
        {
            var agentAddress = Game.Game.instance.States.AgentState.address;
            var avatarAddress = Game.Game.instance.States.CurrentAvatarState.address;
            if (!Game.Game.instance.CurrentPlanetId.HasValue)
            {
                var errorString = "$SeasonPassServiceManager [PrevClaim] Game.Game.instance.CurrentPlanetId is null";
                NcDebug.LogError(errorString);
                onError?.Invoke(errorString);
                return;
            }

            if(!CurrentSeasonPassData.TryGetValue(passType, out var seasonPassSchema))
            {
                var errorString = $"SeasonPassServiceManager [PrevClaim] CurrentSeasonPassData {passType} is null";
                NcDebug.LogError(errorString);
                onError?.Invoke(errorString);
                return;
            }

            Client.PostUserClaimprevAsync(new SeasonPassServiceClient.ClaimRequestSchema
            {
                AgentAddr = agentAddress.ToString(),
                AvatarAddr = avatarAddress.ToString(),
                SeasonIndex = seasonPassSchema.SeasonIndex - 1,
                PassType = passType,
                PlanetId = Enum.Parse<SeasonPassServiceClient.PlanetID>($"_{Game.Game.instance.CurrentPlanetId}"),
                Force = false,
                Prev = true
            },
                (result) => { onSuccess?.Invoke(result); },
                (error) =>
                {
                    NcDebug.LogError($"SeasonPassServiceManager [PrevClaim] error: {error}");
                    onError?.Invoke(error);
                }).AsUniTask().Forget();
        }

        public void GetExp(SeasonPassServiceClient.PassType passType, int level, out int minExp, out int maxExp)
        {
            if (LevelInfos == null
                || !LevelInfos.TryGetValue(passType, out var levelInfoList)
                || levelInfoList.Count - 2 < 0
                || levelInfoList.Count - 1 < 0)
            {
                NcDebug.LogError("[SeasonPassServiceManager] LevelInfos Not Set");
                minExp = 0;
                maxExp = 0;
                return;
            }

            if (level >= levelInfoList.Count)
            {
                minExp = levelInfoList[levelInfoList.Count - 2].Exp;
                maxExp = levelInfoList[levelInfoList.Count - 1].Exp;
                return;
            }

            minExp = level - 1 >= 0 ? levelInfoList[level - 1].Exp : 0;
            maxExp = levelInfoList[level].Exp;
        }

        public string GetSeasonPassPopupViewKey()
        {
            var result = $"SeasonPassCouragePopupViewed {Game.Game.instance.States.CurrentAvatarState.address.ToHex()} SeasonPassID {CurrentSeasonPassData[SeasonPassServiceClient.PassType.CouragePass].Id}";
            return result;
        }

        public void Dispose()
        {
            Client?.Dispose();
        }
    }
}
