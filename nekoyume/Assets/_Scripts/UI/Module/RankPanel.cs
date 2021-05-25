using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;
using Nekoyume.UI.Model;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Nekoyume.L10n;
using Nekoyume.Battle;
using System.Threading.Tasks;
using Nekoyume.GraphQL;
using Libplanet;
using Cysharp.Threading.Tasks;

namespace Nekoyume.UI.Module
{
    public class RankPanel : MonoBehaviour
    {
        public class Model
        {
            private HashSet<Nekoyume.Model.State.RankingInfo> _rankingInfoSet = null;

            public List<AbilityRankingModel> AbilityRankingInfos = null;

            public List<StageRankingModel> StageRankingInfos = null;

            public List<StageRankingModel> MimisbrunnrRankingInfos = null;

            public List<EquipmentRankingModel> WeaponRankingModel = null;

            public Dictionary<int, AbilityRankingModel> AgentAbilityRankingInfos = new Dictionary<int, AbilityRankingModel>();

            public Dictionary<int, StageRankingModel> AgentStageRankingInfos = new Dictionary<int, StageRankingModel>();

            public Dictionary<int, StageRankingModel> AgentMimisbrunnrRankingInfos = new Dictionary<int, StageRankingModel>();

            public Dictionary<int, EquipmentRankingModel> AgentWeaponRankingInfos = new Dictionary<int, EquipmentRankingModel>();

            public async Task Update()
            {
                var rankingMapStates = States.Instance.RankingMapStates;
                _rankingInfoSet = new HashSet<Nekoyume.Model.State.RankingInfo>();
                foreach (var pair in rankingMapStates)
                {
                    var rankingInfo = pair.Value.GetRankingInfos(null);
                    _rankingInfoSet.UnionWith(rankingInfo);
                }

                Debug.LogWarning($"total user count : {_rankingInfoSet.Count()}");

                var sw = new Stopwatch();
                sw.Start();

                LoadAbilityRankingInfos();
                await LoadStageRankingInfos();
                await LoadMimisbrunnrRankingInfos();

                sw.Stop();
                UnityEngine.Debug.LogWarning($"total elapsed : {sw.Elapsed}");
            }

            private void LoadAbilityRankingInfos()
            {
                var characterSheet = Game.Game.instance.TableSheets.CharacterSheet;
                var costumeStatSheet = Game.Game.instance.TableSheets.CostumeStatSheet;

                var rankOffset = 1;
                AbilityRankingInfos = _rankingInfoSet
                    .OrderByDescending(i => i.Level)
                    .Take(100)
                    .Select(rankingInfo =>
                    {
                        var iValue = Game.Game.instance.Agent.GetState(rankingInfo.AvatarAddress);
                        var avatarState = new AvatarState((Bencodex.Types.Dictionary)iValue);
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

            private async Task LoadStageRankingInfos()
            {
                var client = NineChroniclesAPIClient.Instance;
                var query =
                    @"query {
                        stageRanking(limit: 100) {
                            ranking
                            avatarAddress
                            clearedStageId
                            name
                        }
                    }";

                var response = await client.GetObjectAsync<StageRankingResponse>(query);
                StageRankingInfos =
                    response.StageRanking
                    .Select(x =>
                    {
                        var addressString = x.AvatarAddress.Substring(2);
                        var address = new Address(addressString);
                        var iValue = Game.Game.instance.Agent.GetState(address);
                        if (iValue is Bencodex.Types.Null || iValue is null)
                        {
                            Debug.LogError($"Failed to get state of user {address}.");
                            return null;
                        }
                        var avatarState = new AvatarState((Bencodex.Types.Dictionary)iValue);

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

                    var myInfoResponse = await client.GetObjectAsync<StageRankingResponse>(myInfoQuery);
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
                    var iValue = Game.Game.instance.Agent.GetState(address);
                    var avatarState = new AvatarState((Bencodex.Types.Dictionary)iValue);
                    AgentStageRankingInfos[pair.Key] = new StageRankingModel
                    {
                        AvatarState = avatarState,
                        ClearedStageId = myRecord.ClearedStageId,
                        Rank = myRecord.Ranking,
                    };
                }
            }

            private async Task LoadMimisbrunnrRankingInfos()
            {
                var client = NineChroniclesAPIClient.Instance;
                var query =
                    @"query {
                        stageRanking(limit: 100, mimisbrunnr: true) {
                            ranking
                            avatarAddress
                            clearedStageId
                            name
                        }
                    }";

                var response = await client.GetObjectAsync<StageRankingResponse>(query);
                MimisbrunnrRankingInfos =
                    response.StageRanking
                    .Select(x =>
                    {
                        var addressString = x.AvatarAddress.Substring(2);
                        var address = new Address(addressString);
                        var iValue = Game.Game.instance.Agent.GetState(address);
                        var avatarState = new AvatarState((Bencodex.Types.Dictionary)iValue);

                        return new StageRankingModel
                        {
                            AvatarState = avatarState,
                            ClearedStageId = x.ClearedStageId > 0 ?
                                x.ClearedStageId - GameConfig.MimisbrunnrStartStageId + 1 : 0,
                            Rank = x.Ranking,
                        };
                    }).ToList();

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

                    var myInfoResponse = await client.GetObjectAsync<StageRankingResponse>(myInfoQuery);
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
                    var iValue = Game.Game.instance.Agent.GetState(address);
                    var avatarState = new AvatarState((Bencodex.Types.Dictionary)iValue);
                    AgentMimisbrunnrRankingInfos[pair.Key] = new StageRankingModel
                    {
                        AvatarState = avatarState,
                        ClearedStageId = myRecord.ClearedStageId - GameConfig.MimisbrunnrStartStageId + 1,
                        Rank = myRecord.Ranking,
                    };
                }
            }
        }

        private static readonly Model SharedModel = new Model();

        private static Task RankLoadingTask = null;

        [SerializeField]
        private List<Toggle> toggles = new List<Toggle>();

        [SerializeField]
        private TextMeshProUGUI firstColumnText = null;

        [SerializeField]
        private TextMeshProUGUI secondColumnText = null;

        [SerializeField]
        private RankScroll rankScroll = null;

        [SerializeField]
        private RankCellPanel myInfoCell = null;

        public const int RankingBoardDisplayCount = 100;

        private readonly ReactiveProperty<RankCategory> _currentCategory = new ReactiveProperty<RankCategory>();

        private readonly Dictionary<RankCategory, (string, string)> _rankColumnMap = new Dictionary<RankCategory, (string, string)>
        {
            { RankCategory.Ability, ("UI_CP", "UI_LEVEL") },
            { RankCategory.Stage, ("UI_STAGE", null)},
            { RankCategory.Mimisburnnr, ("UI_STAGE", null) },
            { RankCategory.Weapon, ("UI_CP", null) },
        };

        public static void UpdateSharedModel()
        {
            var model = SharedModel;

            RankLoadingTask = model.Update();
        }

        public void Initialize()
        {
            _currentCategory.Subscribe(UpdateCategory)
                .AddTo(gameObject);

            var currentCategory = RankCategory.Ability;
            foreach (var toggle in toggles)
            {
                var innerCategory = currentCategory;
                if (toggle is ToggleDropdown toggleDropdown)
                {
                    var subElements = toggleDropdown.items;
                    foreach (var element in subElements)
                    {
                        var innerCategory2 = innerCategory;
                        element.onValueChanged.AddListener(value =>
                        {
                            if (value)
                            {
                                _currentCategory.SetValueAndForceNotify(innerCategory2);
                            }
                        });
                        ++innerCategory;
                        ++currentCategory;
                    }

                    toggleDropdown.onValueChanged.AddListener(value =>
                    {
                        if (value)
                        {
                            var firstElement = subElements.First();
                            if (firstElement is null)
                            {
                                Debug.LogError($"No sub element exists in {toggleDropdown.name}.");
                                return;
                            }

                            if (firstElement.isOn)
                            {
                                firstElement.onValueChanged.Invoke(true);
                            }
                            else
                            {
                                firstElement.isOn = true;
                            }
                        }
                    });
                }
                else
                {
                    toggle.onValueChanged.AddListener(value =>
                    {
                        if (value)
                        {
                            _currentCategory.SetValueAndForceNotify(innerCategory);
                        }
                    });
                    ++currentCategory;
                }
            }
        }

        public void Show()
        {
            ToggleFirstElement();
        }

        private void ToggleFirstElement()
        {
            var firstElement = toggles.First();
            if (firstElement is null)
            {
                Debug.LogError($"No element exists in {name}");
                return;
            }

            if (firstElement is Toggle)
            {
                firstElement.isOn = true;
            }
            else if (firstElement is ToggleDropdown dropdown)
            {
                var firstSubElement = dropdown.items.First();
                if (firstSubElement is null)
                {
                    Debug.LogError($"No sub element exists in {dropdown.name}");
                    return;
                }

                firstSubElement.isOn = true;
            }

            _currentCategory.SetValueAndForceNotify(RankCategory.Ability);
            
        }

        private void UpdateCategory(RankCategory category)
        {
            UpdateCategoryAsync(category);
        }

        private async void UpdateCategoryAsync(RankCategory category)
        {
            await UniTask.WaitWhile(() => RankLoadingTask is null);
            if (RankLoadingTask.IsFaulted)
            {
                Debug.LogError($"Error loading ranking. Exception : \n{RankLoadingTask.Exception}\n{RankLoadingTask.Exception.StackTrace}");
                return;
            }

            if (!RankLoadingTask.IsCompleted)
            {
                await RankLoadingTask;
            }

            var states = States.Instance;

            if (states.CurrentAvatarState is null)
            {
                return;
            }

            switch (category)
            {
                case RankCategory.Ability:
                    var abilityRankingInfos = SharedModel.AbilityRankingInfos;
                    if (SharedModel.AgentAbilityRankingInfos
                        .TryGetValue(states.CurrentAvatarKey, out var abilityInfo))
                    {
                        myInfoCell.SetDataAsAbility(abilityInfo);
                    }
                    else
                    {
                        myInfoCell.SetEmpty(states.CurrentAvatarState);
                    }

                    rankScroll.Show(abilityRankingInfos, true);
                    break;
                case RankCategory.Stage:
                    var stageRankingInfos = SharedModel.StageRankingInfos;
                    if (SharedModel.AgentStageRankingInfos
                        .TryGetValue(states.CurrentAvatarKey, out var stageInfo))
                    {
                        myInfoCell.SetDataAsStage(stageInfo);
                    }
                    else
                    {
                        myInfoCell.SetEmpty(states.CurrentAvatarState);
                    }

                    rankScroll.Show(stageRankingInfos, true);
                    break;
                case RankCategory.Mimisburnnr:
                    var mimisbrunnrRankingInfos = SharedModel.MimisbrunnrRankingInfos;
                    if (SharedModel.AgentMimisbrunnrRankingInfos
                        .TryGetValue(states.CurrentAvatarKey, out var mimisbrunnrInfo))
                    {
                        myInfoCell.SetDataAsStage(mimisbrunnrInfo);
                    }
                    else
                    {
                        myInfoCell.SetEmpty(states.CurrentAvatarState);
                    }

                    rankScroll.Show(mimisbrunnrRankingInfos, true);
                    break;
                case RankCategory.Weapon:
                    var weaponRankingInfos = SharedModel.WeaponRankingModel;
                    if (weaponRankingInfos is null)
                    {
                        ToggleFirstElement();
                        Widget.Find<SystemPopup>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE", "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
                        return;
                    }

                    break;
                default:
                    break;
            }

            var firstCategory = _rankColumnMap[category].Item1;
            if (firstCategory is null)
            {
                firstColumnText.text = string.Empty;
            }
            else
            {
                firstColumnText.text = firstCategory.StartsWith("UI_") ? L10nManager.Localize(firstCategory) : firstCategory;
            }

            var secondCategory = _rankColumnMap[category].Item2;
            if (secondCategory is null)
            {
                firstColumnText.text = string.Empty;
            }
            else
            {
                secondColumnText.text = secondCategory.StartsWith("UI_") ? L10nManager.Localize(secondCategory) : secondCategory;
            }
        }
    }
}
