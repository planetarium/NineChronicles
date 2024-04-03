using Nekoyume.State;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Nekoyume.GraphQL;

using Debug = UnityEngine.Debug;
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

        public Dictionary<int, AbilityRankingModel> AgentAbilityRankingInfos = new Dictionary<int, AbilityRankingModel>();

        public Dictionary<int, StageRankingModel> AgentStageRankingInfos = new Dictionary<int, StageRankingModel>();

        public Dictionary<int, CraftRankingModel> AgentCraftRankingInfos = new Dictionary<int, CraftRankingModel>();

        public Dictionary<int, Dictionary<ItemSubType, EquipmentRankingModel>> AgentEquipmentRankingInfos =
            new Dictionary<int, Dictionary<ItemSubType, EquipmentRankingModel>>();

        public Task Update(int displayCount)
        {
            var apiClient = Game.Game.instance.ApiClient;

            if (apiClient.IsInitialized)
            {
                return Task.Run(async () =>
                {
                    var sw = new Stopwatch();
                    sw.Stop();
                    sw.Start();
                    await Task.WhenAll(
                        LoadAbilityRankingInfos(apiClient, displayCount),
                        LoadStageRankingInfos(apiClient, displayCount),
                        LoadCraftRankingInfos(apiClient, displayCount),
                        LoadEquipmentRankingInfos(apiClient, displayCount)
                    );
                    IsInitialized = true;
                    sw.Stop();
                    NcDebug.Log($"Ranking updated in {sw.ElapsedMilliseconds}ms.(elapsed)");
                });
            }

            return Task.CompletedTask;
        }

        private async Task LoadAbilityRankingInfos(NineChroniclesAPIClient apiClient, int displayCount)
        {
            var query =
                $@"query {{
                        abilityRanking(limit: {displayCount}) {{
                            ranking
                            avatarAddress
                            name
                            avatarLevel
                            armorId
                            titleId
                            cp
                        }}
                    }}";

            var response = await apiClient.GetObjectAsync<AbilityRankingResponse>(query);
            if (response is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(AbilityRankingResponse)}");
                return;
            }

            AbilityRankingInfos = response.AbilityRanking
                .Select(e =>
                {
                    return new AbilityRankingModel
                    {
                        Rank = e.Ranking,
                        AvatarAddress = e.AvatarAddress,
                        Name = e.Name,
                        AvatarLevel = e.AvatarLevel,
                        ArmorId = e.ArmorId,
                        TitleId = e.TitleId,
                        Cp = e.Cp,
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
                            abilityRanking(avatarAddress: ""{pair.Value.address}"") {{
                                ranking
                                avatarAddress
                                name
                                avatarLevel
                                armorId
                                titleId
                                cp
                            }}
                        }}";

                var myInfoResponse = await apiClient.GetObjectAsync<AbilityRankingResponse>(myInfoQuery);
                if (myInfoResponse is null)
                {
                    NcDebug.LogError("Failed getting my ranking record.");
                    continue;
                }

                var myRecord = myInfoResponse.AbilityRanking.FirstOrDefault();
                if (myRecord is null)
                {
                    NcDebug.LogWarning($"{nameof(AbilityRankingRecord)} not exists.");
                    continue;
                }

                AgentAbilityRankingInfos[pair.Key] = new AbilityRankingModel
                {
                    Rank = myRecord.Ranking,
                    AvatarAddress = myRecord.AvatarAddress,
                    Name = myRecord.Name,
                    AvatarLevel = myRecord.AvatarLevel,
                    ArmorId = myRecord.ArmorId,
                    TitleId = myRecord.TitleId,
                    Cp = myRecord.Cp,
                };
            }
        }

        private async Task LoadStageRankingInfos(NineChroniclesAPIClient apiClient, int displayCount)
        {
            var query =
                $@"query {{
                        stageRanking(limit: {displayCount}) {{
                            ranking
                            avatarAddress
                            name
                            avatarLevel
                            armorId
                            titleId
                            clearedStageId
                        }}
                    }}";

            var response = await apiClient.GetObjectAsync<StageRankingResponse>(query);
            if (response is null)
            {
                NcDebug.LogError($"Failed getting response : {nameof(StageRankingResponse)}");
                return;
            }

            StageRankingInfos = response.StageRanking
                .Select(e =>
                {
                    return new StageRankingModel
                    {
                        Rank = e.Ranking,
                        AvatarAddress = e.AvatarAddress,
                        Name = e.Name,
                        AvatarLevel = e.AvatarLevel,
                        ArmorId = e.ArmorId,
                        TitleId = e.TitleId,
                        ClearedStageId = e.ClearedStageId,
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
                                name
                                avatarLevel
                                armorId
                                titleId
                                clearedStageId
                            }}
                        }}";

                var myInfoResponse = await apiClient.GetObjectAsync<StageRankingResponse>(myInfoQuery);
                if (myInfoResponse is null)
                {
                    NcDebug.LogError("Failed getting my ranking record.");
                    continue;
                }

                var myRecord = myInfoResponse.StageRanking.FirstOrDefault();
                if (myRecord is null)
                {
                    NcDebug.LogWarning($"{nameof(StageRankingRecord)} not exists.");
                    continue;
                }

                AgentStageRankingInfos[pair.Key] = new StageRankingModel
                {
                    Rank = myRecord.Ranking,
                    AvatarAddress = myRecord.AvatarAddress,
                    Name = myRecord.Name,
                    AvatarLevel = myRecord.AvatarLevel,
                    ArmorId = myRecord.ArmorId,
                    TitleId = myRecord.TitleId,
                    ClearedStageId = myRecord.ClearedStageId,
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
                        CraftCount = e.CraftCount,
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
                    CraftCount = myRecord.CraftCount,
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
                    NcDebug.LogError($"Failed getting response : {nameof(EquipmentRankingResponse)}");
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
                        EquipmentId = myRecord.EquipmentId,
                    };
                }
            }
        }
    }
}
