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

            public Dictionary<int, AbilityRankingModel> AgentAbilityRankingInfos = new Dictionary<int, AbilityRankingModel>();

            public Dictionary<int, StageRankingModel> AgentStageRankingInfos = new Dictionary<int, StageRankingModel>();

            public Dictionary<int, StageRankingModel> AgentMimisbrunnrRankingInfos = new Dictionary<int, StageRankingModel>();

            public void Update()
            {
                var rankingMapStates = States.Instance.RankingMapStates;
                SharedModel._rankingInfoSet = new HashSet<Nekoyume.Model.State.RankingInfo>();
                foreach (var pair in rankingMapStates)
                {
                    var rankingInfo = pair.Value.GetRankingInfos(null);
                    SharedModel._rankingInfoSet.UnionWith(rankingInfo);
                }

                Debug.LogWarning($"total user count : {SharedModel._rankingInfoSet.Count()}");

                var sw = new Stopwatch();
                sw.Start();

                LoadAbilityRankingInfos();

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

            private void LoadStageRankingInfo()
            {
                var orderedAvatarStates = _rankingInfoSet
                    .Select(rankingInfo =>
                    {
                        var iValue = Game.Game.instance.Agent.GetState(rankingInfo.AvatarAddress);
                        var avatarState = new AvatarState((Bencodex.Types.Dictionary)iValue);

                        return avatarState;
                    })
                    .ToList()
                    .OrderByDescending(x => x.worldInformation.TryGetLastClearedStageId(out var id) ? id : 0)
                    .ToList();

                foreach (var pair in States.Instance.AvatarStates)
                {
                    var avatarState = pair.Value;
                    var avatarAddress = avatarState.address;
                    var index = orderedAvatarStates.FindIndex(i => i.address.Equals(avatarAddress));
                    if (index >= 0)
                    {
                        var stageProgress = avatarState.worldInformation.TryGetLastClearedStageId(out var id) ? id : 0;

                        AgentStageRankingInfos[pair.Key] =
                            new StageRankingModel()
                            {
                                Rank = index + 1,
                                AvatarState = avatarState,
                                Stage = stageProgress,
                            };
                    }
                }

                StageRankingInfos = orderedAvatarStates
                    .Take(RankingBoardDisplayCount)
                    .Select(avatarState =>
                    {
                        var stageProgress = avatarState.worldInformation.TryGetLastClearedStageId(out var id) ? id : 0;

                        return new StageRankingModel()
                        {
                            AvatarState = avatarState,
                            Stage = stageProgress,
                        };
                    }).ToList();
            }

            private void LoadMimisbrunnrRankingInfo()
            {
                var orderedAvatarStates = _rankingInfoSet
                    .Select(rankingInfo =>
                    {
                        var iValue = Game.Game.instance.Agent.GetState(rankingInfo.AvatarAddress);
                        var avatarState = new AvatarState((Bencodex.Types.Dictionary)iValue);

                        return avatarState;
                    })
                    .ToList()
                    .OrderByDescending(x => x.worldInformation.TryGetLastClearedMimisbrunnrStageId(out var id) ? id : 0)
                    .ToList();

                foreach (var pair in States.Instance.AvatarStates)
                {
                    var avatarState = pair.Value;
                    var avatarAddress = avatarState.address;
                    var index = orderedAvatarStates.FindIndex(i => i.address.Equals(avatarAddress));
                    if (index >= 0)
                    {
                        var stageProgress = avatarState.worldInformation.TryGetLastClearedMimisbrunnrStageId(out var id) ? id : 0;

                        AgentMimisbrunnrRankingInfos[pair.Key] =
                            new StageRankingModel()
                            {
                                Rank = index + 1,
                                AvatarState = avatarState,
                                Stage = stageProgress > 0 ?
                                    stageProgress - GameConfig.MimisbrunnrStartStageId + 1 : 0,
                            };
                    }
                }

                MimisbrunnrRankingInfos = orderedAvatarStates
                    .Take(RankingBoardDisplayCount)
                    .Select(avatarState =>
                    {
                        var stageProgress = avatarState.worldInformation.TryGetLastClearedMimisbrunnrStageId(out var id) ? id : 0;

                        return new StageRankingModel()
                        {
                            AvatarState = avatarState,
                            Stage = stageProgress > 0 ?
                                stageProgress - GameConfig.MimisbrunnrStartStageId + 1 : 0,
                        };
                    }).ToList();
            }
        }

        private static readonly Model SharedModel = new Model();

        private static Task RankLoadingTask = null;

        [SerializeField]
        private Transform cellViewParent = null;

        [SerializeField]
        private List<NCToggle> toggles = new List<NCToggle>();

        [SerializeField]
        private GameObject rankCellPrefab = null;

        [SerializeField]
        private GameObject myInfoPrefab = null;

        [SerializeField]
        private TextMeshProUGUI firstColumnText = null;

        [SerializeField]
        private TextMeshProUGUI secondColumnText = null;

        [SerializeField]
        private RankScroll rankScroll = null;

        [SerializeField]
        private RankCell myInfoCell = null;

        public const int RankingBoardDisplayCount = 100;

        private readonly ReactiveProperty<RankCategory> _currentCategory = new ReactiveProperty<RankCategory>();

        private readonly List<RankCell> _cellViewCache = new List<RankCell>();

        private RankCell _myInfoCellCache = null;

        private readonly Dictionary<RankCategory, (string, string)> _rankColumnMap = new Dictionary<RankCategory, (string, string)>
        {
            { RankCategory.Ability, ("UI_CP", "UI_LEVEL") },
            { RankCategory.Stage, ("UI_STAGE", null)},
            { RankCategory.Mimisburnnr, ("UI_STAGE", null) },
        };

        public static void UpdateSharedModel()
        {
            RankLoadingTask = Task.Run(() =>
            {
                var model = SharedModel;
                model.Update();

                return
                    model.AbilityRankingInfos != null ||
                    model.StageRankingInfos != null ||
                    model.MimisbrunnrRankingInfos != null;
            });
        }

        public void Initialize()
        {
            _currentCategory.Subscribe(UpdateCategory)
                .AddTo(gameObject);

            var currentCategory = RankCategory.Ability;
            foreach (var toggle in toggles)
            {
                var innerCategory = currentCategory;
                if (toggle is NCToggleDropdown toggleDropdown)
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

            CacheCellViews();
        }

        private void CacheCellViews()
        {
            GameObject gameObject;
            RankCell rankCell;
            for (int i = 0; i < RankingBoardDisplayCount; ++i)
            {
                gameObject = Instantiate(rankCellPrefab, cellViewParent);
                rankCell = gameObject.GetComponent<RankCell>();
                _cellViewCache.Add(rankCell);
                gameObject.SetActive(false);
            }

            gameObject = Instantiate(myInfoPrefab, cellViewParent);
            rankCell = gameObject.GetComponent<RankCell>();
            _myInfoCellCache = rankCell;
            gameObject.SetActive(false);
        }

        public void Show()
        {
            ToggleFirstElement();
            _currentCategory.SetValueAndForceNotify(RankCategory.Ability);
        }

        private void ToggleFirstElement()
        {
            var firstElement = toggles.First();
            if (firstElement is null)
            {
                Debug.LogError($"No element exists in {name}");
                return;
            }

            if (firstElement is NCToggle)
            {
                firstElement.isOn = true;
            }
            else if (firstElement is NCToggleDropdown dropdown)
            {
                var firstSubElement = dropdown.items.First();
                if (firstSubElement is null)
                {
                    Debug.LogError($"No sub element exists in {dropdown.name}");
                    return;
                }

                firstSubElement.isOn = true;
            }
        }

        private void UpdateCategory(RankCategory category)
        {
            UpdateCategoryAsync(category);
        }

        private async void UpdateCategoryAsync(RankCategory category)
        {
            if (_myInfoCellCache is null)
            {
                return;
            }

            var states = States.Instance;

            if (RankLoadingTask.IsFaulted)
            {
                Debug.LogError($"Error loading ranking. Exception : \n{RankLoadingTask.Exception}\n{RankLoadingTask.Exception.StackTrace}");
                return;
            }

            if (!RankLoadingTask.IsCompleted)
            {
                await RankLoadingTask;
            }

            _myInfoCellCache.gameObject.SetActive(false);
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
                    if (stageRankingInfos is null)
                    {
                        ToggleFirstElement();
                        Widget.Find<SystemPopup>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE", "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
                        return;
                    }

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
                    if (mimisbrunnrRankingInfos is null)
                    {
                        ToggleFirstElement();
                        Widget.Find<SystemPopup>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE", "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
                        return;
                    }

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
                default:
                    _cellViewCache.ForEach(cell => cell.gameObject.SetActive(false));
                    break;
            }

            var firstCategory = _rankColumnMap[category].Item1;
            firstColumnText.text = firstCategory.StartsWith("UI_") ? L10nManager.Localize(firstCategory) : firstCategory;
            var secondCategory = _rankColumnMap[category].Item2;
            secondColumnText.text = secondCategory.StartsWith("UI_") ? L10nManager.Localize(secondCategory) : secondCategory;
        }

        #region Shared Model Update

        #endregion
    }
}
