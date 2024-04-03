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
using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    using Nekoyume.Model.Item;
    using UniRx;

    public class RankPopup : PopupWidget
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
        private List<Button> maintenancingToggles = null;

        public const int RankingBoardDisplayCount = 100;

        private readonly Dictionary<RankCategory, Toggle> _toggleMap = new Dictionary<RankCategory, Toggle>();

        private readonly Dictionary<RankCategory, (string, string)> _rankColumnMap = new Dictionary<RankCategory, (string, string)>
        {
            { RankCategory.Ability, ("UI_CP", "UI_LEVEL") },
            { RankCategory.Stage, ("UI_STAGE", null)},
            { RankCategory.Crafting, ("UI_COUNTS_CRAFTED", null) },
            { RankCategory.EquipmentWeapon, ("UI_CP", "UI_NAME") },
            { RankCategory.EquipmentArmor, ("UI_CP", "UI_NAME") },
            { RankCategory.EquipmentBelt, ("UI_CP", "UI_NAME") },
            { RankCategory.EquipmentNecklace, ("UI_CP", "UI_NAME") },
            { RankCategory.EquipmentRing, ("UI_CP", "UI_NAME") },
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

                toggle.Toggle.onClickToggle.AddListener(AudioController.PlayClick);
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

                dropDown.onClickToggle.AddListener(AudioController.PlayClick);
            }

            foreach (var button in maintenancingToggles)
            {
                button.OnClickAsObservable()
                    .Subscribe(_ => AlertMaintenancing())
                    .AddTo(gameObject);
            }

            refreshButton.onClick.AsObservable()
                .Subscribe(_ =>
                {
                    UpdateSharedModel();
                    UpdateCategory(RankCategory.Ability, true);
                })
                .AddTo(gameObject);

            closeButton.onClick.AsObservable()
                .Subscribe(_ =>
                {
                    Close();
                    AudioController.PlayClick();
                })
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            var firstCategory = categoryToggles.FirstOrDefault(x => x.Category != RankCategory.Maintenance);
            UpdateCategory(firstCategory.Category, true);
        }

        private void UpdateCategory(RankCategory category, bool toggleOn = false)
        {
            UpdateCategoryAsync(category, toggleOn);
        }

        private async void UpdateCategoryAsync(RankCategory category, bool toggleOn)
        {
            preloadingObject.SetActive(true);

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
                NcDebug.LogError($"Error loading ranking. Exception : \n{RankLoadingTask.Exception}\n{RankLoadingTask.Exception.StackTrace}");
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

            preloadingObject.SetActive(false);
            missingObject.SetActive(false);
            refreshObject.SetActive(false);

            if (!isApiLoaded)
            {
                return;
            }

            switch (category)
            {
                case RankCategory.Ability:
                    SetScroll(SharedModel.AgentAbilityRankingInfos, SharedModel.AbilityRankingInfos);
                    break;
                case RankCategory.Stage:
                    SetScroll(SharedModel.AgentStageRankingInfos, SharedModel.StageRankingInfos);
                    break;
                case RankCategory.Crafting:
                    SetScroll(SharedModel.AgentCraftRankingInfos, SharedModel.CraftRankingInfos);
                    break;
                case RankCategory.EquipmentWeapon:
                    SetEquipmentScroll(ItemSubType.Weapon);
                    break;
                case RankCategory.EquipmentArmor:
                    SetEquipmentScroll(ItemSubType.Armor);
                    break;
                case RankCategory.EquipmentBelt:
                    SetEquipmentScroll(ItemSubType.Belt);
                    break;
                case RankCategory.EquipmentNecklace:
                    SetEquipmentScroll(ItemSubType.Necklace);
                    break;
                case RankCategory.EquipmentRing:
                    SetEquipmentScroll(ItemSubType.Ring);
                    break;
                default:
                    break;
            }
        }

        private void SetScroll<T>(
            IReadOnlyDictionary<int, T> myRecordMap,
            IEnumerable<T> rankingInfos)
            where T : RankingModel
        {
            if (rankingInfos is null)
            {
                Find<Alert>().Show("UI_ERROR", "UI_RANKING_CATEGORY_ERROR");
                rankingInfos = new List<T>();
            }

            var states = States.Instance;
            if (myRecordMap.TryGetValue(states.CurrentAvatarKey, out var rankingInfo))
            {
                myInfoCell.SetData(rankingInfo);
            }
            else
            {
                myInfoCell.SetEmpty(states.CurrentAvatarState);
            }

            rankScroll.Show(rankingInfos, true);
        }

        private void SetEquipmentScroll(ItemSubType type)
        {
            var states = States.Instance;
            var rankingInfos = SharedModel.EquipmentRankingInfosMap[type];
            if (SharedModel.AgentEquipmentRankingInfos
                .TryGetValue(states.CurrentAvatarKey, out var equipmentRankingMap))
            {
                var rankingInfo = equipmentRankingMap[type];
                myInfoCell.SetData(rankingInfo);
            }
            else
            {
                myInfoCell.SetEmpty(states.CurrentAvatarState);
            }

            rankScroll.Show(rankingInfos, true);
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
                    NcDebug.LogError($"No sub element exists in {dropdown.name}");
                    return;
                }

                firstSubElement.isOn = true;
            }
        }

        private void AlertMaintenancing()
        {
            Find<TitleOneButtonSystem>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE", "UI_MAINTENANCE");
        }
    }
}
