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
        public Dictionary<SeasonPassServiceClient.PassType, Dictionary<SeasonPassServiceClient.ActionType, int>> SeasonExpPointAmount { get; private set; } = new Dictionary<SeasonPassServiceClient.PassType, Dictionary<SeasonPassServiceClient.ActionType, int>>();

        public SeasonPassServiceClient Client { get; private set; }

        public Dictionary<SeasonPassServiceClient.PassType, SeasonPassServiceClient.SeasonPassSchema> CurrentSeasonPassData { get; private set; } = new Dictionary<SeasonPassServiceClient.PassType, SeasonPassServiceClient.SeasonPassSchema>();
        public Dictionary<SeasonPassServiceClient.PassType, List<SeasonPassServiceClient.LevelInfoSchema>> LevelInfos { get; private set; } = new Dictionary<SeasonPassServiceClient.PassType, List<SeasonPassServiceClient.LevelInfoSchema>>();
        public Dictionary<SeasonPassServiceClient.PassType, SeasonPassServiceClient.UserSeasonPassSchema> UserSeasonPassDatas = new();

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

        public System.Action UpdatedUserDatas;

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

                    await FetchLevelInfoDataWithRetry(passType);
                }
            }

        }

        private bool IsCurrentSeasonPassValid(SeasonPassServiceClient.PassType passType)
        {
            if (CurrentSeasonPassData != null &&
                CurrentSeasonPassData.TryGetValue(passType, out var SeasonPassSchema) &&
                DateTime.TryParse(SeasonPassSchema.EndTimestamp, out var endDateTime) &&
                endDateTime >= DateTime.Now)
            {
                return true;
            }

            //월드패스의경우 종료기간이 없기때문에 리워드가 있으면 유효하다고 판단한다.
            if (passType == SeasonPassServiceClient.PassType.WorldClearPass &&
                CurrentSeasonPassData.TryGetValue(passType, out var seasonPassSchema) &&
                seasonPassSchema.RewardList.Count > 0)
            {
                NcDebug.LogWarning($"SeasonPassServiceManager IsCurrentSeasonPassValid [WorldClearPass] is null");
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

                await Client.GetSeasonpassCurrentAsync(Game.Game.instance.CurrentPlanetId.ToString(), passType, (result) =>
                {
                    CurrentSeasonPassData[passType] = result;

                    // 용기 시즌패스의 경우만 종료일을 설정하고 남은 시간을 갱신한다.
                    // 현재는 용기시즌패스, 어드벤쳐보스 시즌패스 모두 같은 시즌주기시간을 사용하기때문.
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

            InitializeSeasonPassDatasAsync().Forget();

            Game.Lobby.OnLobbyEnterEvent += () => AvatarStateRefreshAsync().AsUniTask().Forget();
        }

        private async UniTaskVoid InitializeSeasonPassDatasAsync()
        {
            await FetchCurrentSeasonPassDatas();
            foreach (var passType in passTypes)
            {
                await FetchExpInfoDataWithRetry(passType, 1);
            }
        }

        public int ExpPointAmount(SeasonPassServiceClient.PassType passType, SeasonPassServiceClient.ActionType actionType)
        {
            if (SeasonExpPointAmount.TryGetValue(passType, out var expPointAmountDic))
            {
                if (expPointAmountDic.TryGetValue(actionType, out var expPointAmount))
                {
                    return expPointAmount;
                }
            }
            return 0;
        }

        private async Task FetchExpInfoDataWithRetry(SeasonPassServiceClient.PassType passType, int maxRetries = 3)
        {
            int retryCount = 0;

            int seasonIndex = 0;
            if (CurrentSeasonPassData.TryGetValue(passType, out var seasonPassSchema))
            {
                seasonIndex = seasonPassSchema.SeasonIndex;
            }

            while (retryCount < maxRetries)
            {
                var tcs = new TaskCompletionSource<bool>();

                await Client.GetSeasonpassExpAsync(passType, seasonIndex, (result) =>
                {
                    foreach (var item in result)
                    {
                        if(!SeasonExpPointAmount.TryGetValue(passType, out var expPointAmountDic))
                        {
                            SeasonExpPointAmount.Add(passType, new Dictionary<SeasonPassServiceClient.ActionType, int>());
                        }

                        SeasonExpPointAmount[passType][item.ActionType] = item.Exp;
                    }
                    tcs.SetResult(true);
                },
                (error) =>
                {
                    NcDebug.LogError($"SeasonPassServiceManager FetchExpInfoDataWithRetry [GetSeasonpassExpAsync] [{passType}] error: {error}");
                    tcs.SetResult(false);
                });

                if (await tcs.Task)
                    break;

                retryCount++;
            }
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

            await Client.GetUserStatusAllAsync(
                Game.Game.instance.CurrentPlanetId.ToString(),
                Game.Game.instance.States.AgentState.address.ToString(),
                Game.Game.instance.States.CurrentAvatarState.address.ToString(),
                (result) =>
                {
                    foreach (var userSeasonPassSchema in result)
                    {
                        if (CurrentSeasonPassData.TryGetValue(userSeasonPassSchema.SeasonPass.PassType, out var currentSeasonPassData))
                        {
                            if (currentSeasonPassData.SeasonIndex == userSeasonPassSchema.SeasonPass.SeasonIndex)
                            {
                                //현재 시즌 정보
                                UserSeasonPassDatas[userSeasonPassSchema.SeasonPass.PassType] = userSeasonPassSchema;
                                if (userSeasonPassSchema.Level > userSeasonPassSchema.LastNormalClaim || (userSeasonPassSchema.IsPremium && userSeasonPassSchema.Level > userSeasonPassSchema.LastPremiumClaim))
                                {
                                    HasClaimPassType.Add(userSeasonPassSchema.SeasonPass.PassType);
                                }
                                else
                                {
                                    HasClaimPassType.Remove(userSeasonPassSchema.SeasonPass.PassType);
                                }
                            }
                            else if (currentSeasonPassData.SeasonIndex - 1 == userSeasonPassSchema.SeasonPass.SeasonIndex)
                            {
                                //이전 시즌 정보
                                if (DateTime.TryParse(userSeasonPassSchema.ClaimLimitTimestamp, out var claimLimitTimestamp))
                                {
                                    PrevSeasonClaimEndDates[userSeasonPassSchema.SeasonPass.PassType] = claimLimitTimestamp;
                                }
                                else
                                {
                                    Debug.LogError($"SeasonPassServiceManager [FetchUserAllStatus] PrevSeasonClaimEndDates {userSeasonPassSchema.SeasonPass.PassType} is Not DateTime {userSeasonPassSchema.ClaimLimitTimestamp}");
                                }

                                //이전시즌 보상의경우 프리미엄일때만 체크합니다.
                                if (userSeasonPassSchema.IsPremium
                                    && userSeasonPassSchema.Level > userSeasonPassSchema.LastPremiumClaim
                                    && !string.IsNullOrEmpty(userSeasonPassSchema.ClaimLimitTimestamp)
                                    && DateTime.TryParse(userSeasonPassSchema.ClaimLimitTimestamp, out var claimLimitTime)
                                    && DateTime.UtcNow.AddHours(9) < claimLimitTime)
                                {
                                    HasPrevClaimPassType.Add(userSeasonPassSchema.SeasonPass.PassType);
                                }
                                else
                                {
                                    HasPrevClaimPassType.Remove(userSeasonPassSchema.SeasonPass.PassType);
                                }
                            }
                            else
                            {
                                Debug.LogError($"SeasonPassServiceManager [FetchUserAllStatus] CurrentSeasonPassData {userSeasonPassSchema.SeasonPass.PassType} SeasonIndex is not matched PrevSeasonIndex is {currentSeasonPassData.SeasonIndex - 1}");
                            }
                        }
                        else
                        {
                            Debug.LogError($"SeasonPassServiceManager [FetchUserAllStatus] CurrentSeasonPassData {userSeasonPassSchema.SeasonPass.PassType} is null");
                        }
                    }
                },
                (error) =>
                {
                    Debug.LogError($"SeasonPassServiceManager [FetchUserAllStatus] error: {error}");
                });

            UpdatedUserDatas?.Invoke();
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
                || levelInfoList.Count - 2 < 0)
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
