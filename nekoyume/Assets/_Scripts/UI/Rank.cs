using Cysharp.Threading.Tasks;
using Nekoyume.EnumType;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    public class Rank : Widget
    {
        private static readonly Model.Rank SharedModel = new Model.Rank();

        private static Task RankLoadingTask = null;

        public static void UpdateSharedModel()
        {
            var model = SharedModel;

            RankLoadingTask = model.Update(RankingBoardDisplayCount);
        }

        public override WidgetType WidgetType => WidgetType.Tooltip;

        [SerializeField]
        private Button closeButton = null;

        [SerializeField]
        private Model.Rank rankPanel = null;

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

        [SerializeField]
        private GameObject emptyObject = null;

        public const int RankingBoardDisplayCount = 100;

        private readonly ReactiveProperty<RankCategory> _currentCategory = new ReactiveProperty<RankCategory>();

        private readonly Dictionary<RankCategory, (string, string)> _rankColumnMap = new Dictionary<RankCategory, (string, string)>
        {
            { RankCategory.Ability, ("UI_CP", "UI_LEVEL") },
            { RankCategory.Stage, ("UI_STAGE", null)},
            { RankCategory.Mimisburnnr, ("UI_STAGE", null) },
            { RankCategory.Weapon, ("UI_CP", null) },
        };

        public override void Initialize()
        {
            base.Initialize();

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
                            if (subElements is null || !subElements.Any())
                            {
                                Debug.LogError($"No sub element exists in {toggleDropdown.name}.");
                                return;
                            }

                            var firstElement = subElements.First();

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

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
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

            if (!SharedModel.IsInitialized)
            {
                emptyObject.SetActive(true);
                myInfoCell.SetEmpty(states.CurrentAvatarState);
                return;
            }
            emptyObject.SetActive(false);

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
                        Find<SystemPopup>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE", "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
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
                secondColumnText.text = string.Empty;
            }
            else
            {
                secondColumnText.text = secondCategory.StartsWith("UI_") ? L10nManager.Localize(secondCategory) : secondCategory;
            }
        }
    }
}
