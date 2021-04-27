using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class RankingInfo
    {
        public string Name;
        public Address AvatarAddress;
        public AvatarState AvatarState;
    }

    public class AbilityRankingInfo : RankingInfo
    {
        public int Cp;
        public int Level;
    }

    public class StageRankingInfo : RankingInfo
    {
        public int StageId;
    }

    public class RankPanel : MonoBehaviour
    {
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
        private RankCell myInfoCell = null;

        [SerializeField]
        private int displayCount = 100;

        private HashSet<Nekoyume.Model.State.RankingInfo> rankingInfoSet = null;

        private List<RankCell> _cellViewCache = new List<RankCell>();

        private List<AbilityRankingInfo> _abilityRankingInfos = null;

        private List<StageRankingInfo> _stageRankingInfos = null;

        private List<StageRankingInfo> _mimisbrunnrRankingInfos = null;

        private (int rank, AbilityRankingInfo rankingInfo) _myAbilityRankingInfo;

        private (int rank, StageRankingInfo rankingInfo) _myStageRankingInfo;

        private (int rank, StageRankingInfo rankingInfo) _myMimisbrunnrRankingInfo;

        private readonly Dictionary<RankCategory, (string, string)> _rankColumnMap = new Dictionary<RankCategory, (string, string)>
        {
            { RankCategory.Ability, ("UI_CP", "UI_LEVEL") },
            { RankCategory.Stage, ("UI_STAGE", null)},
            { RankCategory.Mimisburnnr, ("UI_STAGE", null) },
        };

        public void Initialize()
        {
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
                                UpdateCategory(innerCategory2);
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
                            UpdateCategory(innerCategory);
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
            for (int i = 0; i < displayCount; ++i)
            {
                gameObject = Instantiate(rankCellPrefab, cellViewParent);
                rankCell = gameObject.GetComponent<RankCell>();
                _cellViewCache.Add(rankCell);
                gameObject.SetActive(false);
            }

            gameObject = Instantiate(myInfoPrefab, cellViewParent);
            rankCell = gameObject.GetComponent<RankCell>();
            _cellViewCache.Add(rankCell);
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            if (rankingInfoSet is null)
            {
                var rankingMapStates = States.Instance.RankingMapStates;
                rankingInfoSet = new HashSet<Nekoyume.Model.State.RankingInfo>();
                foreach (var pair in rankingMapStates)
                {
                    var rankingInfo = pair.Value.GetRankingInfos(null);
                    rankingInfoSet.UnionWith(rankingInfo);
                }
            }
        }

        public void Show()
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
            switch (category)
            {
                case RankCategory.Ability:
                    if (_abilityRankingInfos is null)
                    {
                        LoadAbilityRankingInfos();
                    }

                    for (int i = 0; i < _abilityRankingInfos.Count(); ++i)
                    {
                        if (i >= displayCount)
                        {
                            break;
                        }

                        var rank = i + 1;
                        _cellViewCache[i].SetDataAsAbility(rank, _abilityRankingInfos[i]);
                    }
                    myInfoCell.SetDataAsAbility(_myAbilityRankingInfo.rank, _myAbilityRankingInfo.rankingInfo);
                    break;
                case RankCategory.Stage:
                    if (_stageRankingInfos is null)
                    {
                        LoadStageRankingInfo();
                    }

                    for (int i = 0; i < _stageRankingInfos.Count(); ++i)
                    {
                        if (i >= displayCount)
                        {
                            break;
                        }

                        var rank = i + 1;
                        _cellViewCache[i].SetDataAsStage(rank, _stageRankingInfos[i]);
                    }
                    myInfoCell.SetDataAsStage(_myStageRankingInfo.rank, _myStageRankingInfo.rankingInfo);
                    break;
                case RankCategory.Mimisburnnr:
                    if (_mimisbrunnrRankingInfos is null)
                    {
                        LoadMimisbrunnrRankingInfo();
                    }

                    for (int i = 0; i < _mimisbrunnrRankingInfos.Count(); ++i)
                    {
                        if (i >= displayCount)
                        {
                            break;
                        }

                        var rank = i + 1;
                        _cellViewCache[i].SetDataAsStage(rank, _mimisbrunnrRankingInfos[i]);
                    }
                    myInfoCell.SetDataAsStage(_myMimisbrunnrRankingInfo.rank, _myMimisbrunnrRankingInfo.rankingInfo);
                    break;
                default:
                    _cellViewCache.ForEach(cell => cell.gameObject.SetActive(false));
                    break;
            }

            firstColumnText.text = _rankColumnMap[category].Item1;
            secondColumnText.text = _rankColumnMap[category].Item2;
        }

        private void LoadAbilityRankingInfos()
        {
            var weeklyArenaState = States.Instance.WeeklyArenaState;

            var abilityRankingInfos = rankingInfoSet
                .OrderByDescending(i =>
                {
                    var avatarAddress = i.AvatarAddress;
                    var arenaInfo = weeklyArenaState.GetArenaInfo(avatarAddress);

                    return arenaInfo is null ? 0 : arenaInfo.CombatPoint;
                })
                .ThenByDescending(c => c.Level)
                .ToList();

            var myAvatarState = States.Instance.CurrentAvatarState;
            var myAvatarAddress = myAvatarState.address;
            var myInfoIndex = abilityRankingInfos.FindIndex(i => i.AvatarAddress.Equals(myAvatarAddress));
            if (myInfoIndex >= 0)
            {
                var myInfo = abilityRankingInfos[myInfoIndex];
                var myArenaInfo = weeklyArenaState.GetArenaInfo(myAvatarAddress);
                var avatarNameWithNoHash = myArenaInfo.AvatarName.Split().First();

                _myAbilityRankingInfo = (
                    myInfoIndex + 1,
                    new AbilityRankingInfo()
                    {
                        Name = avatarNameWithNoHash,
                        AvatarState = myAvatarState,
                        AvatarAddress = myAvatarAddress,
                        Cp = myArenaInfo is null ? 0 : myArenaInfo.CombatPoint,
                        Level = myInfo.Level
                    });
            }

            _abilityRankingInfos = abilityRankingInfos
                .Take(displayCount)
                .Select(i =>
                {
                    var avatarAddress = i.AvatarAddress;
                    var arenaInfo = weeklyArenaState.GetArenaInfo(avatarAddress);
                    var avatarNameWithNoHash = arenaInfo.AvatarName.Split().First();

                    var iValue = Game.Game.instance.Agent.GetState(avatarAddress);
                    var avatarState = new AvatarState((Bencodex.Types.Dictionary)iValue);

                    return new AbilityRankingInfo()
                    {
                        Name = avatarNameWithNoHash,
                        AvatarState = avatarState,
                        AvatarAddress = avatarAddress,
                        Cp = arenaInfo.CombatPoint,
                        Level = arenaInfo.Level
                    };
                }).ToList();
        }

        private void LoadStageRankingInfo()
        {
            var stageRankingInfos = rankingInfoSet
                .Select(rankingInfo =>
                {
                    var iValue = Game.Game.instance.Agent.GetState(rankingInfo.AvatarAddress);
                    var avatarState = new AvatarState((Bencodex.Types.Dictionary) iValue);

                    return (avatarState, rankingInfo);
                })
                .OrderByDescending(x => x.avatarState.worldInformation
                    .TryGetLastClearedStageId(out var id) ? id : 0)
                    .ToList();

            var myAvatarState = States.Instance.CurrentAvatarState;
            var myAvatarAddress = myAvatarState.address;
            var myInfoIndex = stageRankingInfos.FindIndex(i => i.avatarState.address.Equals(myAvatarAddress));
            if (myInfoIndex >= 0)
            {
                var (avatarState, _) = stageRankingInfos[myInfoIndex];
                var myStageProgress = myAvatarState.worldInformation.TryGetLastClearedStageId(out var id) ? id : 0;

                _myStageRankingInfo = (
                    myInfoIndex + 1,
                    new StageRankingInfo()
                    {
                        Name = avatarState.name,
                        AvatarState = avatarState,
                        AvatarAddress = avatarState.address,
                        StageId = myStageProgress,
                    });
            }

            _stageRankingInfos = stageRankingInfos
                .Take(displayCount)
                .Select(i =>
                {
                    var avatarState = i.avatarState;
                    var stageProgress = avatarState.worldInformation.TryGetLastClearedStageId(out var id) ? id : 0;

                    return new StageRankingInfo()
                    {
                        Name = avatarState.name,
                        AvatarState = avatarState,
                        AvatarAddress = avatarState.address,
                        StageId = stageProgress,
                    };
                }).ToList();
        }

        private void LoadMimisbrunnrRankingInfo()
        {
            var mimisbrunnrRankingInfos = rankingInfoSet
                .Select(rankingInfo =>
                {
                    var iValue = Game.Game.instance.Agent.GetState(rankingInfo.AvatarAddress);
                    var avatarState = new AvatarState((Bencodex.Types.Dictionary) iValue);

                    return (avatarState, rankingInfo);
                })
                .OrderByDescending(x => x.avatarState.worldInformation
                    .TryGetLastClearedMimisbrunnrStageId(out var id) ? id : 0)
                    .ToList();

            var myAvatarState = States.Instance.CurrentAvatarState;
            var myAvatarAddress = myAvatarState.address;
            var myInfoIndex = mimisbrunnrRankingInfos.FindIndex(i => i.avatarState.address.Equals(myAvatarAddress));
            if (myInfoIndex >= 0)
            {
                var (avatarState, rankingInfo) = mimisbrunnrRankingInfos[myInfoIndex];
                var myStageProgress = myAvatarState.worldInformation.TryGetLastClearedMimisbrunnrStageId(out var id) ? id : 0;

                _myMimisbrunnrRankingInfo = (
                    myInfoIndex + 1,
                    new StageRankingInfo()
                    {
                        Name = avatarState.name,
                        AvatarState = avatarState,
                        AvatarAddress = avatarState.address,
                        StageId = myStageProgress,
                    });
            }

            _mimisbrunnrRankingInfos = mimisbrunnrRankingInfos
                .Take(displayCount)
                .Select(i =>
                {
                    var avatarState = i.avatarState;
                    var stageProgress = avatarState.worldInformation.TryGetLastClearedMimisbrunnrStageId(out var id) ? id : 0;

                    return new StageRankingInfo()
                    {
                        Name = avatarState.name,
                        AvatarState = avatarState,
                        AvatarAddress = avatarState.address,
                        StageId = stageProgress,
                    };
                }).ToList();
        }
    }
}
