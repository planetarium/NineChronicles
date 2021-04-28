using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;
using Nekoyume.UI.Model;
using Debug = UnityEngine.Debug;

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

        public const int RankingBoardDisplayCount = 100;

        private List<RankCell> _cellViewCache = new List<RankCell>();

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
            for (int i = 0; i < RankingBoardDisplayCount; ++i)
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
            UpdateCategoryAsync(category);
        }

        private async void UpdateCategoryAsync(RankCategory category)
        {
            var states = States.Instance;
            var loadingTask = Game.Game.instance.RankLoadingTask;

            if (loadingTask.IsFaulted)
            {
                Debug.LogError($"Error loading ranking. Exception : \n{loadingTask.Exception}\n{loadingTask.Exception.StackTrace}");
                return;
            }

            if (!loadingTask.IsCompleted)
            {
                await loadingTask;
            }

            switch (category)
            {
                case RankCategory.Ability:
                    var abilityRankingInfos = states.AbilityRankingInfos;
                    if (states.AgentAbilityRankingInfos.TryGetValue(states.CurrentAvatarKey, out var abilityInfo))
                    {
                        myInfoCell.SetDataAsAbility(abilityInfo);
                    }

                    for (int i = 0; i < RankingBoardDisplayCount; ++i)
                    {
                        if (i >= abilityRankingInfos.Count())
                        {
                            _cellViewCache[i].gameObject.SetActive(false);
                            break;
                        }

                        var rank = i + 1;
                        abilityRankingInfos[i].Rank = rank;
                        _cellViewCache[i].SetDataAsAbility(abilityRankingInfos[i]);
                        _cellViewCache[i].gameObject.SetActive(true);
                    }
                    break;
                case RankCategory.Stage:
                    var stageRankingInfos = states.StageRankingInfos;
                    if (states.AgentStageRankingInfos.TryGetValue(states.CurrentAvatarKey, out var stageInfo))
                    {
                        myInfoCell.SetDataAsStage(stageInfo);
                    }

                    for (int i = 0; i < RankingBoardDisplayCount; ++i)
                    {
                        if (i >= stageRankingInfos.Count())
                        {
                            _cellViewCache[i].gameObject.SetActive(false);
                            break;
                        }

                        var rank = i + 1;
                        stageRankingInfos[i].Rank = rank;
                        _cellViewCache[i].SetDataAsStage(stageRankingInfos[i]);
                        _cellViewCache[i].gameObject.SetActive(true);
                    }
                    break;
                case RankCategory.Mimisburnnr:
                    var mimisbrunnrRankingInfos = states.MimisbrunnrRankingInfos;
                    if (states.AgentMimisbrunnrRankingInfos.TryGetValue(states.CurrentAvatarKey, out var mimisbrunnrInfo))
                    {
                        myInfoCell.SetDataAsStage(mimisbrunnrInfo);
                    }

                    for (int i = 0; i < RankingBoardDisplayCount; ++i)
                    {
                        if (i >= mimisbrunnrRankingInfos.Count())
                        {
                            _cellViewCache[i].gameObject.SetActive(false);
                            break;
                        }

                        var rank = i + 1;
                        mimisbrunnrRankingInfos[i].Rank = rank;
                        _cellViewCache[i].SetDataAsStage(mimisbrunnrRankingInfos[i]);
                        _cellViewCache[i].gameObject.SetActive(true);
                    }
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
