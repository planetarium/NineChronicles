using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using System.Linq;
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
        private int displayCount = 100;

        private List<RankCell> _cellViewCache = new List<RankCell>();

        private List<(string, Address, int, int)> _expRankingInfos = null;

        private List<(string, Address, int, int)> ExpRankingInfos {
            get
            {
                if (_expRankingInfos is null)
                {
                    LoadRankingInfos();
                }

                if (_expRankingInfos is null)
                {
                    Debug.LogError("Failed loading ExpRakingInfos.");
                }

                return _expRankingInfos;
            }
        }

        private readonly List<RankCategory> _rankCategories = new List<RankCategory>()
        {
            RankCategory.Ability,
            RankCategory.Stage,
            RankCategory.Mimisburnnr,
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
                        innerCategory = currentCategory;
                        element.onValueChanged.AddListener(value =>
                        {
                            if (value)
                            {
                                UpdateCategory(innerCategory);
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

        private void LoadRankingInfos()
        {
            var rankingMapStates = States.Instance.RankingMapStates;
            var weeklyArenaState = States.Instance.WeeklyArenaState;

            var rankingInfoSet = new HashSet<Nekoyume.Model.State.RankingInfo>();
            foreach (var pair in rankingMapStates)
            {
                var rankingInfo = pair.Value.GetRankingInfos(null);
                rankingInfoSet.UnionWith(rankingInfo);
            }

            _expRankingInfos = rankingInfoSet
                .OrderByDescending(i =>
                {
                    var avatarAddress = i.AvatarAddress;
                    var arenaInfo = weeklyArenaState.GetArenaInfo(avatarAddress);

                    return arenaInfo is null ? 0 : arenaInfo.CombatPoint;
                })
                .ThenByDescending(c => c.Level)
                .Take(displayCount)
                .Select(i =>
                {
                    var avatarAddress = i.AvatarAddress;
                    var arenaInfo = weeklyArenaState.GetArenaInfo(avatarAddress);
                    return (arenaInfo.AvatarName, avatarAddress, arenaInfo.CombatPoint, arenaInfo.Level);
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
                    for (int i = 0; i < ExpRankingInfos.Count(); ++i)
                    {
                        if (i >= displayCount)
                        {
                            break;
                        }

                        var rank = i + 1;
                        (string name, Address avatarAddress, int level, int cp) = ExpRankingInfos[i];
                        _cellViewCache[i].SetDataAsAbility(rank, name, avatarAddress, level, cp);
                    }
                    break;
                default:
                    _cellViewCache.ForEach(cell => cell.gameObject.SetActive(false));
                    break;
            }
        }
    }
}
