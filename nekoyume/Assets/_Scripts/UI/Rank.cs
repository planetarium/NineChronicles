using Cysharp.Threading.Tasks;
using Nekoyume.EnumType;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    using UniRx;

    public class Rank : Widget
    {
        [Serializable]
        private struct CategoryToggle
        {
            public Toggle Toggle;
            public RankCategory Category;
        }

        private static readonly Model.Rank SharedModel = new Model.Rank();

        private static Task RankLoadingTask = null;

        public static void UpdateSharedModel()
        {
            var model = SharedModel;

            RankLoadingTask = model.Update(RankingBoardDisplayCount);
        }

        public override WidgetType WidgetType => WidgetType.Popup;

        [SerializeField]
        private Button closeButton = null;

        [SerializeField]
        private TextMeshProUGUI firstColumnText = null;

        [SerializeField]
        private TextMeshProUGUI secondColumnText = null;

        [SerializeField]
        private RankScroll rankScroll = null;

        [SerializeField]
        private RankCellPanel myInfoCell = null;

        [SerializeField]
        private GameObject preloadingObject = null;

        [SerializeField]
        private GameObject missingObject = null;

        [SerializeField]
        private TextMeshProUGUI missingText = null;

        [SerializeField]
        private GameObject refreshObject = null;

        [SerializeField]
        private Button refreshButton = null;

        [SerializeField]
        private List<CategoryToggle> categoryToggles = null;

        [SerializeField]
        private List<ToggleDropdown> categoryDropdowns = null;

        [SerializeField]
        private List<Button> notImplementedToggles = null;

        public const int RankingBoardDisplayCount = 100;

        private readonly Dictionary<RankCategory, Toggle> _toggleMap = new Dictionary<RankCategory, Toggle>();

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

            foreach (var toggle in categoryToggles)
            {
                if (!_toggleMap.ContainsKey(toggle.Category))
                {
                    _toggleMap[toggle.Category] = toggle.Toggle;
                }

                toggle.Toggle.onValueChanged.AddListener(value =>
                {
                    if (value)
                    {
                        UpdateCategory(toggle.Category);
                    }
                });
            }

            foreach (var dropDown in categoryDropdowns)
            {
                if (dropDown.items is null ||
                    !dropDown.items.Any())
                {
                    return;
                }

                dropDown.onValueChanged.AddListener(value =>
                {
                    if (value)
                    {
                        var firstElement = dropDown.items.FirstOrDefault();
                        firstElement.isOn = true;
                        firstElement.onValueChanged.Invoke(true);
                    }
                });
            }

            foreach (var button in notImplementedToggles)
            {
                button.OnClickAsObservable()
                    .Subscribe(_ => AlertNotImplemented())
                    .AddTo(gameObject);
            }

            refreshButton.onClick.AsObservable()
                .Subscribe(_ =>
                {
                    UpdateSharedModel();
                    UpdateCategory(RankCategory.Ability, true);
                })
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            UpdateCategory(RankCategory.Ability, true);
        }

        private void UpdateCategory(RankCategory category, bool toggleOn = false)
        {
            UpdateCategoryAsync(category, toggleOn);
        }

        private async void UpdateCategoryAsync(RankCategory category, bool toggleOn)
        {
            preloadingObject.SetActive(true);
            if (category == RankCategory.Weapon)
            {
                Find<SystemPopup>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE", "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
                return;
            }

            if (toggleOn)
            {
                ToggleCategory(category);
            }

            await UniTask.WaitWhile(() => RankLoadingTask is null);

            var states = States.Instance;

            if (!RankLoadingTask.IsCompleted)
            {
                missingObject.SetActive(true);
                refreshObject.SetActive(false);
                missingText.text = L10nManager.Localize("UI_PRELOADING_MESSAGE");
                myInfoCell.SetEmpty(states.CurrentAvatarState);
                await RankLoadingTask;
            }

            if (RankLoadingTask.IsFaulted)
            {
                missingObject.SetActive(false);
                refreshObject.SetActive(true);
                Debug.LogError($"Error loading ranking. Exception : \n{RankLoadingTask.Exception}\n{RankLoadingTask.Exception.StackTrace}");
                myInfoCell.SetEmpty(states.CurrentAvatarState);
                return;
            }

            var isApiLoaded = SharedModel.IsInitialized;
            if (!isApiLoaded)
            {
                missingObject.SetActive(true);
                refreshObject.SetActive(false);
                myInfoCell.SetEmpty(states.CurrentAvatarState);
                return;
            }

            preloadingObject.SetActive(false);
            missingObject.SetActive(false);
            refreshObject.SetActive(false);

            switch (category)
            {
                case RankCategory.Ability:
                    if (!isApiLoaded)
                    {
                        break;
                    }

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
                    if (!isApiLoaded)
                    {
                        break;
                    }

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
                    if (!isApiLoaded)
                    {
                        break;
                    }

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
                        Find<SystemPopup>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE", "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
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

        private void ToggleCategory(RankCategory category)
        {
            var toggle = _toggleMap[category];

            if (toggle is Toggle)
            {
                var dropdown = toggle.GetComponentInParent<ToggleDropdown>();
                if (dropdown)
                {
                    dropdown.isOn = true;
                }
                toggle.isOn = true;
            }
            else if (toggle is ToggleDropdown dropdown)
            {
                var firstSubElement = dropdown.items.FirstOrDefault();
                if (firstSubElement is null)
                {
                    Debug.LogError($"No sub element exists in {dropdown.name}");
                    return;
                }

                firstSubElement.isOn = true;
            }
        }

        private void AlertNotImplemented()
        {
            Find<SystemPopup>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE", "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
        }
    }
}
