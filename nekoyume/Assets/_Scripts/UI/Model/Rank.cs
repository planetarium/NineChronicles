using Nekoyume.Model.State;
using Nekoyume.State;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using System.Diagnostics;
using Nekoyume.Battle;
using System.Threading.Tasks;
using Nekoyume.GraphQL;
using Libplanet;
using Cysharp.Threading.Tasks;

using Debug = UnityEngine.Debug;

namespace Nekoyume.UI.Model
{
    public class Rank
    {
        public bool IsInitialized { get; private set; } = false;

        public List<AbilityRankingModel> AbilityRankingInfos = null;

        public List<StageRankingModel> StageRankingInfos = null;

        public List<StageRankingModel> MimisbrunnrRankingInfos = null;

        public List<EquipmentRankingModel> WeaponRankingModel = null;

        public Dictionary<int, AbilityRankingModel> AgentAbilityRankingInfos = new Dictionary<int, AbilityRankingModel>();

        public Dictionary<int, StageRankingModel> AgentStageRankingInfos = new Dictionary<int, StageRankingModel>();

        public Dictionary<int, StageRankingModel> AgentMimisbrunnrRankingInfos = new Dictionary<int, StageRankingModel>();

        public Dictionary<int, EquipmentRankingModel> AgentWeaponRankingInfos = new Dictionary<int, EquipmentRankingModel>();

        private HashSet<Nekoyume.Model.State.RankingInfo> _rankingInfoSet = null;

        public Task Update(int displayCount)
        {
            var rankingMapStates = States.Instance.RankingMapStates;
            _rankingInfoSet = new HashSet<Nekoyume.Model.State.RankingInfo>();
            foreach (var pair in rankingMapStates)
            {
                var rankingInfo = pair.Value.GetRankingInfos(null);
                _rankingInfoSet.UnionWith(rankingInfo);
            }

            Debug.LogWarning($"total user count : {_rankingInfoSet.Count()}");
            var apiClient = Game.Game.instance.ApiClient;

            if (apiClient.IsInitialized)
            {
                return Task.Run(async () =>
                {
                    var sw = new Stopwatch();
                    sw.Start();

                    LoadAbilityRankingInfos(displayCount);
                    await Task.WhenAll(
                        LoadStageRankingInfos(apiClient, displayCount),
                        LoadMimisbrunnrRankingInfos(apiClient, displayCount)
                    );
                    IsInitialized = true;
                    sw.Stop();
                    UnityEngine.Debug.LogWarning($"total elapsed : {sw.Elapsed}");
                });
            }

            return Task.CompletedTask;
        }

        private void LoadAbilityRankingInfos(int displayCount)
        {
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;

            var rankOffset = 1;
            AbilityRankingInfos = _rankingInfoSet
                .OrderByDescending(i => i.Level)
                .Take(displayCount)
                .Select(rankingInfo =>
                {
                    var avatarState = States.Instance.GetAvatarStateV2(rankingInfo.AvatarAddress);
                    var cp = CPHelper.GetCPV2(avatarState, characterSheet, costumeStatSheet);

                    return new AbilityRankingModel()
                    {
                        AvatarState = avatarState,
                        Cp = cp,
                    };
                })
                .ToList()
                .OrderByDescending(i => i.Cp)
                .ThenByDescending(i => i.AvatarState.level)
                .ToList();
            AbilityRankingInfos.ForEach(i => i.Rank = rankOffset++);

            foreach (var pair in States.Instance.AvatarStates)
            {
                var avatarState = pair.Value;
                var avatarAddress = avatarState.address;
                var index = AbilityRankingInfos.FindIndex(i => i.AvatarState.address.Equals(avatarAddress));
                if (index >= 0)
                {
                    var info = AbilityRankingInfos[index];

                    AgentAbilityRankingInfos[pair.Key] =
                        new AbilityRankingModel()
                        {
                            Rank = index + 1,
                            AvatarState = avatarState,
                            Cp = info.Cp,
                        };
                }
                else
                {
                    var cp = CPHelper.GetCPV2(avatarState, characterSheet, costumeStatSheet);

                    AgentAbilityRankingInfos[pair.Key] =
                        new AbilityRankingModel()
                        {
                            AvatarState = avatarState,
                            Cp = cp,
                        };
                }
            }
        }

        private async Task LoadStageRankingInfos(NineChroniclesAPIClient apiClient, int displayCount)
        {
            var query =
                $@"query {{
                        stageRanking(limit: {displayCount}) {{
                            ranking
                            avatarAddress
                            clearedStageId
                            name
                        }}
                    }}";

            var response = await apiClient.GetObjectAsync<StageRankingResponse>(query);
            StageRankingInfos =
                response.StageRanking
                .Select(x =>
                {
                    var addressString = x.AvatarAddress.Substring(2);
                    var address = new Address(addressString);
                    var avatarState = States.Instance.GetAvatarStateV2(address);

                    return new StageRankingModel
                    {
                        AvatarState = avatarState,
                        ClearedStageId = x.ClearedStageId,
                        Rank = x.Ranking,
                    };
                })
                .Where(x => x != null)
                .ToList();

            foreach (var pair in States.Instance.AvatarStates)
            {
                var myInfoQuery =
                    $@"query {{
                            stageRanking(avatarAddress: ""{pair.Value.address}"") {{
                                ranking
                                avatarAddress
                                clearedStageId
                                name
                            }}
                        }}";

                var myInfoResponse = await apiClient.GetObjectAsync<StageRankingResponse>(myInfoQuery);
                if (myInfoResponse is null)
                {
                    Debug.LogError("Failed getting my ranking record.");
                    return;
                }

                var myRecord = myInfoResponse.StageRanking.FirstOrDefault();
                if (myRecord is null)
                {
                    Debug.LogWarning($"{nameof(StageRankingRecord)} not exists.");
                    return;
                }

                var addressString = myRecord.AvatarAddress.Substring(2);
                var address = new Address(addressString);
                var avatarState = States.Instance.GetAvatarStateV2(address);
                AgentStageRankingInfos[pair.Key] = new StageRankingModel
                {
                    AvatarState = avatarState,
                    ClearedStageId = myRecord.ClearedStageId,
                    Rank = myRecord.Ranking,
                };
            }
        }

        private async Task LoadMimisbrunnrRankingInfos(NineChroniclesAPIClient apiClient, int displayCount)
        {
            var query =
                $@"query {{
                        stageRanking(limit: {displayCount}, mimisbrunnr: true) {{
                            ranking
                            avatarAddress
                            clearedStageId
                            name
                        }}
                    }}";

            var response = await apiClient.GetObjectAsync<StageRankingResponse>(query);
            MimisbrunnrRankingInfos =
                response.StageRanking
                .Select(x =>
                {
                    var addressString = x.AvatarAddress.Substring(2);
                    var address = new Address(addressString);
                    var avatarState = States.Instance.GetAvatarStateV2(address);

                    return new StageRankingModel
                    {
                        AvatarState = avatarState,
                        ClearedStageId = x.ClearedStageId > 0 ?
                            x.ClearedStageId - GameConfig.MimisbrunnrStartStageId + 1 : 0,
                        Rank = x.Ranking,
                    };
                })
                .Where(x => x != null)
                .ToList();

            foreach (var pair in States.Instance.AvatarStates)
            {
                var myInfoQuery =
                    $@"query {{
                            stageRanking(avatarAddress: ""{pair.Value.address}"", mimisbrunnr: true) {{
                                ranking
                                avatarAddress
                                clearedStageId
                                name
                            }}
                        }}";

                var myInfoResponse = await apiClient.GetObjectAsync<StageRankingResponse>(myInfoQuery);
                if (myInfoResponse is null)
                {
                    Debug.LogError("Failed getting my ranking record.");
                    return;
                }

                var myRecord = myInfoResponse.StageRanking.FirstOrDefault();
                if (myRecord is null)
                {
                    Debug.LogWarning($"Mimisbrunnr {nameof(StageRankingRecord)} not exists.");
                    return;
                }

                var addressString = myRecord.AvatarAddress.Substring(2);
                var address = new Address(addressString);
                var avatarState = States.Instance.GetAvatarStateV2(address);
                AgentMimisbrunnrRankingInfos[pair.Key] = new StageRankingModel
                {
                    AvatarState = avatarState,
                    ClearedStageId = myRecord.ClearedStageId - GameConfig.MimisbrunnrStartStageId + 1,
                    Rank = myRecord.Ranking,
                };
            }
        }
    }
}
