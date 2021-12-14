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

using Debug = UnityEngine.Debug;
using Nekoyume.Model.Item;

namespace Nekoyume.UI.Model
{
    public class Rank
    {
        public bool IsInitialized { get; private set; } = false;

        public List<AbilityRankingModel> AbilityRankingInfos = null;

        public List<StageRankingModel> StageRankingInfos = null;

        public List<StageRankingModel> MimisbrunnrRankingInfos = null;

        public List<CraftRankingModel> CraftRankingInfos = null;

        public Dictionary<ItemSubType, List<EquipmentRankingModel>> EquipmentRankingInfosMap = null;

        public Dictionary<int, AbilityRankingModel> AgentAbilityRankingInfos = new Dictionary<int, AbilityRankingModel>();

        public Dictionary<int, StageRankingModel> AgentStageRankingInfos = new Dictionary<int, StageRankingModel>();

        public Dictionary<int, StageRankingModel> AgentMimisbrunnrRankingInfos = new Dictionary<int, StageRankingModel>();

        public Dictionary<int, CraftRankingModel> AgentCraftRankingInfos = new Dictionary<int, CraftRankingModel>();

        public Dictionary<int, Dictionary<ItemSubType, EquipmentRankingModel>> AgentEquipmentRankingInfos =
            new Dictionary<int, Dictionary<ItemSubType, EquipmentRankingModel>>();

        private HashSet<Nekoyume.Model.State.RankingInfo> _rankingInfoSet = null;

        private bool _rankingMapLoaded;

        public Task Update(int displayCount)
        {
            var apiClient = Game.Game.instance.ApiClient;

            if (apiClient.IsInitialized)
            {
                return Task.Run(async () =>
                {
                    //if (!_rankingMapLoaded)
                    //{
                    //    var addressList = new List<Address>();
                    //    for (var i = 0; i < RankingState.RankingMapCapacity; ++i)
                    //    {
                    //        var address = RankingState.Derive(0);
                    //        addressList.Add(address);
                    //    }

                    //    var iValues = await Game.Game.instance.Agent.GetStateBulk(addressList);
                    //    foreach (var kv in iValues)
                    //    {
                    //        var mapState = kv.Value is Bencodex.Types.Dictionary serialized
                    //            ? new RankingMapState(serialized)
                    //            : new RankingMapState(kv.Key);
                    //        States.Instance.SetRankingMapStates(mapState);
                    //    }

                    //    var rankingMapStates = States.Instance.RankingMapStates;
                    //    _rankingInfoSet = new HashSet<Nekoyume.Model.State.RankingInfo>();
                    //    foreach (var pair in rankingMapStates)
                    //    {
                    //        var rankingInfo = pair.Value.GetRankingInfos(null);
                    //        _rankingInfoSet.UnionWith(rankingInfo);
                    //    }

                    //    _rankingMapLoaded = true;
                    //}

                    //Debug.LogWarning($"total user count : {_rankingInfoSet.Count()}");

                    var sw = new Stopwatch();
                    sw.Start();

                    await Task.WhenAll(
                        //LoadAbilityRankingInfos(displayCount),
                        LoadStageRankingInfos(apiClient, displayCount),
                        LoadMimisbrunnrRankingInfos(apiClient, displayCount),
                        LoadCraftRankingInfos(apiClient, displayCount),
                        LoadEquipmentRankingInfos(apiClient, displayCount)
                    );
                    IsInitialized = true;
                    sw.Stop();
                    UnityEngine.Debug.LogWarning($"total elapsed : {sw.Elapsed}");
                });
            }

            return Task.CompletedTask;
        }

        private async Task LoadAbilityRankingInfos(int displayCount)
        {
            var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
            var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;

            var rankOffset = 1;
            var addressList = _rankingInfoSet.Select(i => i.AvatarAddress).ToList();
            var avatarDictionary = await Game.Game.instance.Agent.GetAvatarStates(addressList);
            AbilityRankingInfos = _rankingInfoSet
                .OrderByDescending(i => i.Level)
                .Take(displayCount)
                .Select(async rankingInfo =>
                {
                    var avatarAddress = rankingInfo.AvatarAddress;
                    if (!avatarDictionary.ContainsKey(avatarAddress))
                    {
                        return null;
                    }

                    var avatarState = avatarDictionary[avatarAddress];

                    return new AbilityRankingModel
                    {
                        AvatarState = avatarState,
                        Cp = CPHelper.GetCPV2(avatarState, characterSheet, costumeStatSheet),
                    };
                })
                .Select(t => t?.Result)
                .Where(e => e != null)
                .ToList()
                .OrderByDescending(i => i.Cp)
                .ThenByDescending(i => i.AvatarState.level)
                .ToList();
            AbilityRankingInfos.ForEach(i => i.Rank = rankOffset++);

            var avatarStates = States.Instance.AvatarStates.ToList();
            foreach (var pair in avatarStates)
            {
                var avatarState = pair.Value;
                var avatarAddress = avatarState.address;
                var index = AbilityRankingInfos.FindIndex(i => i.AvatarState.address.Equals(avatarAddress));
                if (index >= 0)
                {
                    var info = AbilityRankingInfos[index];

                    AgentAbilityRankingInfos[pair.Key] =
                        new AbilityRankingModel
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
                        new AbilityRankingModel
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
            if (response is null)
            {
                Debug.LogError($"Failed getting response : {nameof(StageRankingResponse)}");
                return;
            }

            var addressList = response.StageRanking.Select(i => new Address(i.AvatarAddress.Substring(2)));
            var avatarDict = await Game.Game.instance.Agent.GetAvatarStates(addressList);
            StageRankingInfos = response.StageRanking
                .Select(e =>
                {
                    var addressString = e.AvatarAddress.Substring(2);
                    var address = new Address(addressString);
                    if (!avatarDict.ContainsKey(address))
                    {
                        return null;
                    }

                    var avatarState = avatarDict[address];

                    return new StageRankingModel
                    {
                        AvatarState = avatarState,
                        ClearedStageId = e.ClearedStageId,
                        Rank = e.Ranking,
                    };
                })
                .Select(t => t)
                .Where(e => e != null)
                .ToList();

            var avatarStates = States.Instance.AvatarStates.ToList();
            foreach (var pair in avatarStates)
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
                    continue;
                }

                var myRecord = myInfoResponse.StageRanking.FirstOrDefault();
                if (myRecord is null)
                {
                    Debug.LogWarning($"{nameof(StageRankingRecord)} not exists.");
                    continue;
                }

                AgentStageRankingInfos[pair.Key] = new StageRankingModel
                {
                    AvatarState = pair.Value,
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
            if (response is null)
            {
                Debug.LogError($"Failed getting response : {nameof(StageRankingResponse)}");
                return;
            }

            var addressList = response.StageRanking.Select(i => new Address(i.AvatarAddress.Substring(2)));
            var avatarDict = await Game.Game.instance.Agent.GetAvatarStates(addressList);

            MimisbrunnrRankingInfos = response.StageRanking
                .Select(e =>
                {
                    var addressString = e.AvatarAddress.Substring(2);
                    var address = new Address(addressString);
                    if (!avatarDict.ContainsKey(address))
                    {
                        return null;
                    }

                    var avatarState = avatarDict[address];

                    return new StageRankingModel
                    {
                        AvatarState = avatarState,
                        ClearedStageId = e.ClearedStageId > 0 ?
                            e.ClearedStageId - GameConfig.MimisbrunnrStartStageId + 1 : 0,
                        Rank = e.Ranking,
                    };
                })
                .Select(t => t)
                .Where(e => e != null)
                .ToList();

            var avatarStates = States.Instance.AvatarStates.ToList();
            foreach (var pair in avatarStates)
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
                    continue;
                }

                var myRecord = myInfoResponse.StageRanking.FirstOrDefault();
                if (myRecord is null)
                {
                    Debug.LogWarning($"Mimisbrunnr {nameof(StageRankingRecord)} not exists.");
                    continue;
                }

                AgentMimisbrunnrRankingInfos[pair.Key] = new StageRankingModel
                {
                    AvatarState = pair.Value,
                    ClearedStageId = myRecord.ClearedStageId - GameConfig.MimisbrunnrStartStageId + 1,
                    Rank = myRecord.Ranking,
                };
            }
        }

        private async Task LoadCraftRankingInfos(NineChroniclesAPIClient apiClient, int displayCount)
        {
            var query =
                $@"query {{
                        craftRanking(limit: {displayCount}) {{
                            ranking
                            avatarAddress
                            craftCount
                        }}
                    }}";

            var response = await apiClient.GetObjectAsync<CraftRankingResponse>(query);
            if (response is null)
            {
                Debug.LogError($"Failed getting response : {nameof(CraftRankingResponse)}");
                return;
            }

            var addressList = response.CraftRanking.Select(i => new Address(i.AvatarAddress.Substring(2)));
            var avatarDict = await Game.Game.instance.Agent.GetAvatarStates(addressList);

            CraftRankingInfos = response.CraftRanking
                .Select(e =>
                {
                    var addressString = e.AvatarAddress.Substring(2);
                    var address = new Address(addressString);
                    if (!avatarDict.ContainsKey(address))
                    {
                        return null;
                    }

                    var avatarState = avatarDict[address];

                    return new CraftRankingModel
                    {
                        AvatarState = avatarState,
                        CraftCount = e.CraftCount,
                        Rank = e.Ranking,
                    };
                })
                .Select(t => t)
                .Where(e => e != null)
                .ToList();

            var avatarStates = States.Instance.AvatarStates.ToList();
            foreach (var pair in avatarStates)
            {
                var myInfoQuery =
                    $@"query {{
                            craftRanking(avatarAddress: ""{pair.Value.address}"") {{
                                ranking
                                avatarAddress
                                craftCount
                            }}
                        }}";

                var myInfoResponse = await apiClient.GetObjectAsync<CraftRankingResponse>(myInfoQuery);
                if (myInfoResponse is null)
                {
                    Debug.LogError("Failed getting my ranking record.");
                    continue;
                }

                var myRecord = myInfoResponse.CraftRanking.FirstOrDefault();
                if (myRecord is null)
                {
                    Debug.LogWarning($"{nameof(CraftRankingRecord)} not exists.");
                    continue;
                }

                AgentCraftRankingInfos[pair.Key] = new CraftRankingModel
                {
                    AvatarState = pair.Value,
                    CraftCount = myRecord.CraftCount,
                    Rank = myRecord.Ranking,
                };
            }
        }

        private async Task LoadEquipmentRankingInfos(NineChroniclesAPIClient apiClient, int displayCount)
        {
            var subTypes = new ItemSubType[]
                { ItemSubType.Weapon, ItemSubType.Armor, ItemSubType.Belt, ItemSubType.Necklace, ItemSubType.Ring };
            EquipmentRankingInfosMap = new Dictionary<ItemSubType, List<EquipmentRankingModel>>();

            foreach (var subType in subTypes)
            {
                var query =
                    $@"query {{
                        equipmentRanking(itemSubType: ""{subType}"", limit: {displayCount}) {{
                            ranking
                            avatarAddress
                            level
                            equipmentId
                            cp
                        }}
                    }}";

                var response = await apiClient.GetObjectAsync<EquipmentRankingResponse>(query);
                if (response is null)
                {
                    Debug.LogError($"Failed getting response : {nameof(EquipmentRankingResponse)}");
                    return;
                }

                var addressList = response.EquipmentRanking.Select(i => new Address(i.AvatarAddress.Substring(2)));
                var avatarDict = await Game.Game.instance.Agent.GetAvatarStates(addressList);

                EquipmentRankingInfosMap[subType] = response.EquipmentRanking
                    .Select(e =>
                    {
                        var addressString = e.AvatarAddress.Substring(2);
                        var address = new Address(addressString);
                        if (!avatarDict.ContainsKey(address))
                        {
                            return null;
                        }

                        var avatarState = avatarDict[address];

                        return new EquipmentRankingModel
                        {
                            AvatarState = avatarState,
                            Rank = e.Ranking,
                            Level = e.Level,
                            Cp = e.Cp,
                            EquipmentId = e.EquipmentId,
                        };
                    })
                    .Select(t => t)
                    .Where(e => e != null)
                    .ToList();

                var avatarStates = States.Instance.AvatarStates.ToList();
                foreach (var pair in avatarStates)
                {
                    var myInfoQuery =
                        $@"query {{
                            equipmentRanking(itemSubType: ""{subType}"", avatarAddress: ""{pair.Value.address}"", limit: 1) {{
                                ranking
                                avatarAddress
                                level
                                equipmentId
                                cp
                            }}
                        }}";

                    var myInfoResponse = await apiClient.GetObjectAsync<EquipmentRankingResponse>(myInfoQuery);
                    if (myInfoResponse is null)
                    {
                        Debug.LogError("Failed getting my ranking record.");
                        continue;
                    }

                    var myRecord = myInfoResponse.EquipmentRanking.FirstOrDefault();
                    if (myRecord is null)
                    {
                        Debug.LogWarning($"{nameof(EquipmentRankingRecord)} not exists.");
                        continue;
                    }

                    if (!AgentEquipmentRankingInfos.ContainsKey(pair.Key))
                    {
                        AgentEquipmentRankingInfos[pair.Key] = new Dictionary<ItemSubType, EquipmentRankingModel>();
                    }

                    AgentEquipmentRankingInfos[pair.Key][subType] = new EquipmentRankingModel
                    {
                        AvatarState = pair.Value,
                        Rank = myRecord.Ranking,
                        Level = myRecord.Level,
                        Cp = myRecord.Cp,
                        EquipmentId = myRecord.EquipmentId,
                    };
                }
            }
        }
    }
}
