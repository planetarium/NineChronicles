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

        private List<RankCell> _cellViewCache = new List<RankCell>();

        private List<(string, Address, int, int)> _abilityRankingInfos = null;

        private (int rank, string name, Address avatarAddress, int cp, int level) _myAbilityRankingInfo;

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

        public void LoadRankingInfos()
        {
            var rankingMapStates = States.Instance.RankingMapStates;
            var weeklyArenaState = States.Instance.WeeklyArenaState;

            var rankingInfoSet = new HashSet<Nekoyume.Model.State.RankingInfo>();
            foreach (var pair in rankingMapStates)
            {
                var rankingInfo = pair.Value.GetRankingInfos(null);
                rankingInfoSet.UnionWith(rankingInfo);
            }

            var abilityRankingInfos = rankingInfoSet
                .OrderByDescending(i =>
                {
                    var avatarAddress = i.AvatarAddress;
                    var arenaInfo = weeklyArenaState.GetArenaInfo(avatarAddress);

                    return arenaInfo is null ? 0 : arenaInfo.CombatPoint;
                })
                .ThenByDescending(c => c.Level)
                .ToList();

            var myAvatarAddress = States.Instance.CurrentAvatarState.address;
            var myInfoIndex = abilityRankingInfos.FindIndex(i => i.AvatarAddress.Equals(myAvatarAddress));
            if (myInfoIndex >= 0)
            {
                var myInfo = abilityRankingInfos[myInfoIndex];
                var myArenaInfo = weeklyArenaState.GetArenaInfo(myInfo.AvatarAddress);
                var avatarNameWithNoHash = myArenaInfo.AvatarName.Split().First();

                _myAbilityRankingInfo = (
                    myInfoIndex + 1,
                    avatarNameWithNoHash,
                    myInfo.AvatarAddress,
                    myArenaInfo is null ? 0 : myArenaInfo.CombatPoint,
                    myInfo.Level);
            }

            _abilityRankingInfos = abilityRankingInfos
                .Take(displayCount)
                .Select(i =>
                {
                    var avatarAddress = i.AvatarAddress;
                    var arenaInfo = weeklyArenaState.GetArenaInfo(avatarAddress);
                    var avatarNameWithNoHash = arenaInfo.AvatarName.Split().First();

                    return (avatarNameWithNoHash, avatarAddress, arenaInfo.CombatPoint, arenaInfo.Level);
                }).ToList();
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
                    for (int i = 0; i < _abilityRankingInfos.Count(); ++i)
                    {
                        if (i >= displayCount)
                        {
                            break;
                        }

                        var rank = i + 1;
                        (string name, Address avatarAddress, int cp, int level) = _abilityRankingInfos[i];
                        _cellViewCache[i].SetDataAsAbility(rank, name, avatarAddress, cp, level);
                    }
                    myInfoCell.SetDataAsAbility(
                        _myAbilityRankingInfo.rank,
                        _myAbilityRankingInfo.name,
                        _myAbilityRankingInfo.avatarAddress,
                        _myAbilityRankingInfo.cp,
                        _myAbilityRankingInfo.level);
                    break;
                default:
                    _cellViewCache.ForEach(cell => cell.gameObject.SetActive(false));
                    break;
            }

            firstColumnText.text = _rankColumnMap[category].Item1;
            secondColumnText.text = _rankColumnMap[category].Item2;
        }
    }
}
