using Nekoyume.State;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Nekoyume.ApiClient;
using Nekoyume.GraphQL;
using Nekoyume.Model.Item;

namespace Nekoyume.UI.Model
{
    public class Rank
    {
        public bool IsInitialized { get; private set; } = false;

        public List<AbilityRankingModel> AbilityRankingInfos = null;

        public List<StageRankingModel> StageRankingInfos = null;

        public List<CraftRankingModel> CraftRankingInfos = null;

        public Dictionary<ItemSubType, List<EquipmentRankingModel>> EquipmentRankingInfosMap = null;

        public Dictionary<int, AbilityRankingModel> AgentAbilityRankingInfos = new();

        public Dictionary<int, StageRankingModel> AgentStageRankingInfos = new();

        public Dictionary<int, CraftRankingModel> AgentCraftRankingInfos = new();

        public Dictionary<int, Dictionary<ItemSubType, EquipmentRankingModel>> AgentEquipmentRankingInfos = new();

        public Task Update(int displayCount)
        {
            var apiClient = ApiClients.Instance.MimirClient;

            if (apiClient.IsInitialized)
            {
                return Task.Run(async () =>
                {
                    var sw = new Stopwatch();
                    sw.Stop();
                    sw.Start();
                    await Task.WhenAll(
                        LoadAbilityRankingInfos(apiClient, 0, displayCount),
                        LoadStageRankingInfos(apiClient, 0, displayCount)
                        // LoadCraftRankingInfos(apiClient, displayCount),
                        // LoadEquipmentRankingInfos(apiClient, displayCount)
                    );
                    IsInitialized = true;
                    sw.Stop();
                    NcDebug.Log($"Ranking updated in {sw.ElapsedMilliseconds}ms.(elapsed)");
                });
            }

            return Task.CompletedTask;
        }

        private async Task LoadAbilityRankingInfos(NineChroniclesAPIClient apiClient, int offset, int displayCount)
        {
            string fetchAbilityRankingQuery = $@"
                query FetchAbilityRanking {{
                  adventureCpRanking(skip: {offset}, take: {displayCount}) {{
                    items {{
                      avatar {{
                        armorId
                        portraitId
                        object {{
                          address
                          name
                          level
                        }}
                      }}
                      cp
                    }}
                  }}
                }}";

            var response = await apiClient.GetObjectAsync<AdventureCpRankingResponse>(fetchAbilityRankingQuery);
            if (response is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(AdventureCpRankingResponse)}");
                return;
            }

            AbilityRankingInfos = response.AdventureCpRanking.Items
                .Select((e, index) => new AbilityRankingModel
                {
                    Rank = index + 1,
                    AvatarAddress = e.Avatar.Object.Address,
                    Name = e.Avatar.Object.Name,
                    AvatarLevel = e.Avatar.Object.Level,
                    ArmorId = e.Avatar.ArmorId,
                    TitleId = e.Avatar.PortraitId,
                    Cp = e.Cp,
                })
                .Select(t => t)
                .Where(e => e != null)
                .ToList();

            var avatarStates = States.Instance.AvatarStates.ToList();
            foreach (var pair in avatarStates)
            {
                string fetchMyAdventureCpRankingQuery = $@"
                    query FetchMyAdventureCpRanking {{
                      myAdventureCpRanking(address: ""{pair.Value.address}"") {{
                        rank
                        userDocument {{
                          address
                          cp
                          id
                          storedBlockIndex
                        }}
                      }}
                    }}";

                var myInfoResponse = await apiClient.GetObjectAsync<MyAdventureCpRankingResponse>(fetchMyAdventureCpRankingQuery);
                if (myInfoResponse is null)
                {
                    NcDebug.LogError("Failed getting my ranking record.");
                    continue;
                }

                var myRecord = myInfoResponse.MyAdventureCpRanking;
                if (myRecord is null)
                {
                    NcDebug.LogWarning($"{nameof(AbilityRankingRecord)} not exists.");
                    continue;
                }

                AgentAbilityRankingInfos[pair.Key] = new AbilityRankingModel
                {
                    Rank = myRecord.Rank,
                    AvatarAddress = myRecord.UserDocument.Address,
                    Name = pair.Value.name,
                    AvatarLevel = pair.Value.level,
                    ArmorId = pair.Value.GetArmorId(),
                    TitleId = pair.Value.GetPortraitId(),
                    Cp = myRecord.UserDocument.Cp,
                };
            }
        }

        private async Task LoadStageRankingInfos(NineChroniclesAPIClient apiClient, int offset, int displayCount)
        {
            string fetchStageRanking100Query = $@"
                query FetchStageRanking100 {{
                  worldInformationRanking(skip: {offset}, take: {displayCount}) {{
                    items {{
                      lastStageClearedId
                      avatar {{
                        armorId
                        portraitId
                        object {{
                          address
                          name
                          level
                        }}
                      }}
                    }}
                  }}
                }}";

            var response = await apiClient.GetObjectAsync<WorldInformationRankingResponse>(fetchStageRanking100Query);
            if (response is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(WorldInformationRankingResponse)}");
                return;
            }

            StageRankingInfos = response.WorldInformationRanking.Items
                .Select((e, index) =>
                {
                    return new StageRankingModel
                    {
                        Rank = index + 1,
                        AvatarAddress = e.Avatar.Object.Address,
                        Name = e.Avatar.Object.Name,
                        AvatarLevel = e.Avatar.Object.Level,
                        ArmorId = e.Avatar.ArmorId,
                        TitleId = e.Avatar.PortraitId,
                        ClearedStageId = e.LastStageClearedId,
                    };
                })
                .Select(t => t)
                .Where(e => e != null)
                .ToList();

            var avatarStates = States.Instance.AvatarStates.ToList();
            foreach (var pair in avatarStates)
            {
                string fetchMyStageRankingQuery = $@"
                    query FetchMyStageRanking {{
                      myWorldInformationRanking(address: ""{pair.Value.address}"") {{
                        rank
                        userDocument {{
                          address
                          id
                          lastStageClearedId
                          storedBlockIndex
                        }}
                      }}
                    }}";

                var myInfoResponse = await apiClient.GetObjectAsync<MyWorldInformationRankingResponse>(fetchMyStageRankingQuery);
                if (myInfoResponse is null)
                {
                    NcDebug.LogError("Failed getting my ranking record.");
                    continue;
                }

                var myRecord = myInfoResponse.MyWorldInformationRanking;
                if (myRecord is null)
                {
                    NcDebug.LogWarning($"{nameof(MyWorldInformationRankingResponse)} not exists.");
                    continue;
                }

                AgentStageRankingInfos[pair.Key] = new StageRankingModel
                {
                    Rank = myRecord.Rank,
                    AvatarAddress = myRecord.UserDocument.Address,
                    Name = pair.Value.name,
                    AvatarLevel = pair.Value.level,
                    ArmorId = pair.Value.GetArmorId(),
                    TitleId = pair.Value.GetPortraitId(),
                    ClearedStageId = myRecord.UserDocument.LastStageClearedId,
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
                            name
                            avatarLevel
                            armorId
                            titleId
                            craftCount
                        }}
                    }}";

            var response = await apiClient.GetObjectAsync<CraftRankingResponse>(query);
            if (response is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(CraftRankingResponse)}");
                return;
            }

            CraftRankingInfos = response.CraftRanking
                .Select(e =>
                {
                    return new CraftRankingModel
                    {
                        Rank = e.Ranking,
                        AvatarAddress = e.AvatarAddress,
                        Name = e.Name,
                        AvatarLevel = e.AvatarLevel,
                        ArmorId = e.ArmorId,
                        TitleId = e.TitleId,
                        CraftCount = e.CraftCount
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
                                name
                                avatarLevel
                                armorId
                                titleId
                                craftCount
                            }}
                        }}";

                var myInfoResponse = await apiClient.GetObjectAsync<CraftRankingResponse>(myInfoQuery);
                if (myInfoResponse is null)
                {
                    NcDebug.LogError("Failed getting my ranking record.");
                    continue;
                }

                var myRecord = myInfoResponse.CraftRanking.FirstOrDefault();
                if (myRecord is null)
                {
                    NcDebug.LogWarning($"{nameof(CraftRankingRecord)} not exists.");
                    continue;
                }

                AgentCraftRankingInfos[pair.Key] = new CraftRankingModel
                {
                    Rank = myRecord.Ranking,
                    AvatarAddress = myRecord.AvatarAddress,
                    Name = myRecord.Name,
                    AvatarLevel = myRecord.AvatarLevel,
                    ArmorId = myRecord.ArmorId,
                    TitleId = myRecord.TitleId,
                    CraftCount = myRecord.CraftCount
                };
            }
        }

        private async Task LoadEquipmentRankingInfos(NineChroniclesAPIClient apiClient, int displayCount)
        {
            var subTypes = new ItemSubType[]
            {
                ItemSubType.Weapon, ItemSubType.Armor, ItemSubType.Belt, ItemSubType.Necklace,
                ItemSubType.Ring, ItemSubType.Aura, ItemSubType.Grimoire,
            };
            EquipmentRankingInfosMap = new Dictionary<ItemSubType, List<EquipmentRankingModel>>();

            foreach (var subType in subTypes)
            {
                var query =
                    $@"query {{
                        equipmentRanking(itemSubType: ""{subType}"", limit: {displayCount}) {{
                            ranking
                            avatarAddress
                            name
                            avatarLevel
                            armorId
                            titleId
                            level
                            equipmentId
                            cp
                        }}
                    }}";

                var response = await apiClient.GetObjectAsync<EquipmentRankingResponse>(query);
                if (response is null)
                {
                    NcDebug.LogError($"Failed getting response : {nameof(EquipmentRankingResponse)} for {subType}");
                    return;
                }

                EquipmentRankingInfosMap[subType] = response.EquipmentRanking
                    .Select(e =>
                    {
                        return new EquipmentRankingModel
                        {
                            Rank = e.Ranking,
                            AvatarAddress = e.AvatarAddress,
                            Name = e.Name,
                            AvatarLevel = e.AvatarLevel,
                            ArmorId = e.ArmorId,
                            TitleId = e.TitleId,
                            Level = e.Level,
                            Cp = e.Cp,
                            EquipmentId = e.EquipmentId
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
                                name
                                avatarLevel
                                armorId
                                titleId
                                level
                                equipmentId
                                cp
                            }}
                        }}";

                    var myInfoResponse = await apiClient.GetObjectAsync<EquipmentRankingResponse>(myInfoQuery);
                    if (myInfoResponse is null)
                    {
                        NcDebug.LogError("Failed getting my ranking record.");
                        continue;
                    }

                    var myRecord = myInfoResponse.EquipmentRanking.FirstOrDefault();
                    if (myRecord is null)
                    {
                        NcDebug.LogWarning($"{nameof(EquipmentRankingRecord)} not exists.");
                        continue;
                    }

                    if (!AgentEquipmentRankingInfos.ContainsKey(pair.Key))
                    {
                        AgentEquipmentRankingInfos[pair.Key] = new Dictionary<ItemSubType, EquipmentRankingModel>();
                    }

                    AgentEquipmentRankingInfos[pair.Key][subType] = new EquipmentRankingModel
                    {
                        Rank = myRecord.Ranking,
                        AvatarAddress = myRecord.AvatarAddress,
                        Name = myRecord.Name,
                        AvatarLevel = myRecord.AvatarLevel,
                        ArmorId = myRecord.ArmorId,
                        TitleId = myRecord.TitleId,
                        Level = myRecord.Level,
                        Cp = myRecord.Cp,
                        EquipmentId = myRecord.EquipmentId
                    };
                }
            }
        }
    }
}
