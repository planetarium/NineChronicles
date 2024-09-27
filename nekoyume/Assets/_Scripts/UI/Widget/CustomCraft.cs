using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Cysharp.Threading.Tasks;
using Nekoyume.Action.CustomEquipmentCraft;
using Nekoyume.ApiClient;
using Nekoyume.Battle;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.TableData.CustomEquipmentCraft;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Toggle = Nekoyume.UI.Module.Toggle;

namespace Nekoyume.UI
{
    using UniRx;
    public class CustomCraft : Widget
    {
        [Serializable]
        private class SubTypeButton
        {
            public ItemSubType itemSubType;
            public Toggle toggleButton;
        }

        private enum SubmittableState
        {
            Able,
            RandomOnly,
            InsufficientRelationship,
            InsufficientMaterial,
            InsufficientBalance,
            FullSlot,
            WaitRenderingAction,
        }

#region SerializeField
        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private SubTypeButton[] subTypeButtons;

        [SerializeField]
        private ConditionalCostButton conditionalCostButton;

        [SerializeField]
        private GameObject buttonBlockerObject;

        [SerializeField]
        private Button relationshipHelpButton;

        [SerializeField]
        private Button skillListButton;

        [SerializeField]
        private TextMeshProUGUI outfitNameText;

        [SerializeField]
        private RelationshipGaugeView relationshipGaugeView;

        [SerializeField]
        private CustomOutfitScroll outfitScroll;

        [SerializeField]
        private RequiredItemRecipeView requiredItemRecipeView;

        [SerializeField]
        private Image selectedOutfitImage;

        [SerializeField]
        private GameObject notSelected;

        [SerializeField]
        private GameObject selectedView;

        [SerializeField]
        private GameObject selectedImageView;

        [SerializeField]
        private GameObject selectedSpineView;

        [SerializeField]
        private GameObject craftedCountObject;

        [SerializeField]
        private TextMeshProUGUI craftedCountText;

        [SerializeField]
        private GameObject randomOnlyOutfitInfoObject;

        [SerializeField]
        private Button outfitRatioInfoButton;

        [SerializeField]
        private GameObject emptyScrollObject;

        [SerializeField]
        private GameObject speechBubbleObject;

        [SerializeField]
        private CustomEquipmentStatView statView;

        [SerializeField]
        private GameObject relationshipInfoPopupObject;
#endregion

        private CustomOutfit _selectedOutfit;
        private ItemSubType? _selectedSubType;
        private SubmittableState _submittableState;
        private int _selectedItemId;
        private IDisposable _outfitAnimationDisposable;

        private readonly List<IDisposable> _disposables = new();
        private readonly ConcurrentDictionary<int, long> _craftCountDict = new();

        private const string RelationshipInfoKey = "RELATIONSHIP-INFO-{0}";

        private static string TutorialKey => $"Tutorial_Check_CustomCraft_{Game.Game.instance.States.CurrentAvatarKey}";

        public bool RequiredUpdateCraftCount { get; set; }

        public static bool HasNotification
        {
            get
            {
                var tableSheets = TableSheets.Instance;
                var recipeRow = tableSheets.CustomEquipmentCraftRecipeSheet.Values
                    .OrderBy(row => row.CircleAmount + row.ScrollAmount).First();
                var relationshipRow = tableSheets.CustomEquipmentCraftRelationshipSheet.OrderedList!
                    .First(row => row.Relationship >= ReactiveAvatarState.Relationship);

                var (ncgCost, materialCosts) = CustomCraftHelper.CalculateCraftCost(
                    0, (int)ReactiveAvatarState.Relationship, tableSheets.MaterialItemSheet, recipeRow,
                    relationshipRow,
                    States.Instance.GameConfigState.CustomEquipmentCraftIconCostMultiplier
                );

                if (States.Instance.GoldBalanceState.Gold.MajorUnit < ncgCost)
                {
                    return false;
                }

                var inventory = States.Instance.CurrentAvatarState.inventory;
                foreach (var material in materialCosts)
                {
                    if (!tableSheets.MaterialItemSheet.TryGetValue(material.Key, out var row))
                    {
                        continue;
                    }

                    var itemCount = inventory.TryGetFungibleItems(row.ItemId, out var outFungibleItems)
                        ? outFungibleItems.Sum(e => e.count)
                        : 0;

                    if (material.Value > itemCount)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<CombinationMain>().Show();
            });
            relationshipHelpButton.onClick.AddListener(() =>
            {
                Find<RelationshipInfoPopup>().Show();
            });
            skillListButton.onClick.AddListener(() =>
            {
                Find<CustomCraftInfoPopup>().Show(_selectedSubType ?? ItemSubType.Weapon);
            });
            outfitRatioInfoButton.onClick.AddListener(() =>
            {
                Find<OutfitInfoListPopup>().Show(_selectedSubType ?? ItemSubType.Weapon);
            });
            foreach (var subTypeButton in subTypeButtons)
            {
                subTypeButton.toggleButton.onClickToggle.AddListener(() =>
                    OnItemSubtypeSelected(subTypeButton.itemSubType));
            }
        }

        public override void Initialize()
        {
            outfitScroll.OnClick
                .Subscribe(OnOutfitSelected)
                .AddTo(gameObject);
            conditionalCostButton.OnSubmitSubject
                .Subscribe(_ => OnSubmitCraftButton())
                .AddTo(gameObject);
            conditionalCostButton.OnClickSubject
                .Select(_ => Unit.Default)
                .Merge(conditionalCostButton.OnClickDisabledSubject)
                .Subscribe(_ => OnClickCraftButton())
                .AddTo(gameObject);
            UpdateCraftCount();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            AudioController.instance.PlayMusic(AudioController.MusicCode.CustomCraft);
            if (ReactiveAvatarState.Relationship == 0 &&
                PlayerPrefs.GetInt(TutorialKey, 0) == 0)
            {
                // Play Tutorial - Custom craft (for old user)
                Game.Game.instance.Stage.TutorialController.Play(2010002);
                PlayerPrefs.SetInt(TutorialKey, 1);
            }

            foreach (var subTypeButton in subTypeButtons)
            {
                subTypeButton.toggleButton.isOn = subTypeButton.itemSubType == ItemSubType.Weapon;
            }

            OnItemSubtypeSelected(ItemSubType.Weapon);
            ReactiveAvatarState.Inventory
                .Merge(LoadingHelper.CustomEquipmentCraft
                    .Select<bool, Nekoyume.Model.Item.Inventory>(_ => null))
                .Where(_ => _selectedOutfit != null)
                .Subscribe(_ => OnOutfitSelected(_selectedOutfit))
                .AddTo(_disposables);
            ReactiveAvatarState.ObservableRelationship
                .Subscribe(relationship =>
                {
                    relationshipGaugeView.Set(
                        relationship,
                        Math.Max(TableSheets.Instance.CustomEquipmentCraftRelationshipSheet.OrderedList
                            .FirstOrDefault(row => row.Relationship > relationship).Relationship - 1, 0));
                    ShowRelationshipInfoPopup(relationship);
                })
                .AddTo(_disposables);
            LoadingHelper.CustomEquipmentCraft
                .SubscribeTo(buttonBlockerObject)
                .AddTo(_disposables);

            if (RequiredUpdateCraftCount)
            {
                UpdateCraftCount();
            }

            base.Show(ignoreShowAnimation);
        }

        private void UpdateCraftCount()
        {
            RequiredUpdateCraftCount = false;
            if (ApiClients.Instance.WorldBossClient.IsInitialized)
            {
                UniTask.Run(async () =>
                {
                    var response = await CustomCraftQuery.GetCustomEquipmentCraftIconCountAsync(
                        ApiClients.Instance.WorldBossClient);
                    foreach (var pair in response.customEquipmentCraftIconCount)
                    {
                        _craftCountDict[pair.iconId] = pair.count;
                    }
                }).Forget();
            }
        }

        /// <summary>
        /// 어떤 종류의 장비를 만들지 ItemSubType을 선택하면 실행될 콜백, View 업데이트를 한다
        /// </summary>
        /// <param name="type"></param>
        private void OnItemSubtypeSelected(ItemSubType type)
        {
            notSelected.SetActive(true);
            selectedView.SetActive(false);

            if (_selectedSubType == type)
            {
                return;
            }

            AudioController.instance.PlaySfx(AudioController.SfxCode.Typing);
            _selectedSubType = type;
            _selectedOutfit = null;
            var iconSheetRows = TableSheets.Instance.CustomEquipmentCraftIconSheet.Values;
            var existSubType = iconSheetRows.Any(row => row.ItemSubType == type);
            outfitScroll.ContainerObject.SetActive(existSubType);
            emptyScrollObject.SetActive(!existSubType);
            speechBubbleObject.SetActive(existSubType);
            if (existSubType)
            {
                var scrollData = new List<CustomOutfit> {new(null)};
                scrollData.AddRange(iconSheetRows
                    .Where(row => row.ItemSubType == _selectedSubType)
                    .Select(r => new CustomOutfit(r))
                    .OrderBy(r => r.IconRow.Value.RequiredRelationship)
                    .ThenBy(r => r.IconRow.Value.RandomOnly));
                outfitScroll.UpdateData(scrollData);
            }
        }

        private void OnSubmitCraftButton()
        {
            var combinationSlotsPopup = Find<CombinationSlotsPopup>();
            if (combinationSlotsPopup.TryGetEmptyCombinationSlot(out var slotIndex))
            {
                var recipe = TableSheets.Instance.CustomEquipmentCraftRecipeSheet.Values.First(r =>
                    r.ItemSubType == _selectedSubType);
                var item = (Equipment)ItemFactory.CreateItemUsable(
                    TableSheets.Instance.EquipmentItemSheet[_selectedItemId],
                    Guid.NewGuid(),
                    recipe.RequiredBlock);
                var iconId = _selectedOutfit.IconRow.Value?.IconId ??
                    CustomEquipmentCraft.RandomIconId;
                item.IconId = iconId;
                item.ByCustomCraft = true;
                item.CraftWithRandom = iconId == CustomEquipmentCraft.RandomIconId;
                combinationSlotsPopup.OnSendCombinationAction(
                    slotIndex,
                    recipe.RequiredBlock,
                    itemUsable: item);
                ActionManager.Instance.CustomEquipmentCraft(slotIndex,
                    recipe.Id,
                    iconId)
                    .Subscribe();
                LoadingHelper.CustomEquipmentCraft.Value = true;
                OnOutfitSelected(_selectedOutfit);
                StartCoroutine(CoCombineNPCAnimation(item));
            }
        }

        private void OnClickCraftButton()
        {
            if (_submittableState == SubmittableState.Able)
            {
                return;
            }

            var l10N = _submittableState switch
            {
                SubmittableState.RandomOnly => "RANDOM_ONLY_OUTFIT",
                SubmittableState.InsufficientRelationship => "INSUFFICIENT_RELATIONSHIP",
                SubmittableState.InsufficientMaterial => "NOTIFICATION_NOT_ENOUGH_MATERIALS",
                SubmittableState.InsufficientBalance => "UI_NOT_ENOUGH_NCG",
                SubmittableState.FullSlot => "NOTIFICATION_NOT_ENOUGH_SLOTS",
                SubmittableState.WaitRenderingAction => "CUSTOM_CRAFT_WAITING_NOTI",
            };

            OneLineSystem.Push(MailType.System, L10nManager.Localize(l10N), NotificationCell.NotificationType.Alert);
        }

        private void OnOutfitSelected(CustomOutfit outfit)
        {
            _outfitAnimationDisposable?.Dispose();
            var oldSelectedOutfit = _selectedOutfit;
            if (oldSelectedOutfit != null)
            {
                oldSelectedOutfit.Selected.Value = false;
            }

            _selectedOutfit = outfit;
            _selectedOutfit.Selected.Value = true;

            notSelected.SetActive(false);
            selectedView.SetActive(true);
            // 외형 랜덤 선택
            var hasSelectedOutfit = _selectedOutfit.IconRow.Value != null;
            var randomOnly = _selectedOutfit.RandomOnly.Value;
            var iconId = hasSelectedOutfit
                ? _selectedOutfit.IconRow.Value.IconId
                : CustomEquipmentCraft.RandomIconId;
            outfitNameText.SetText(hasSelectedOutfit
                ? L10nManager.LocalizeCustomItemName(iconId)
                : L10nManager.Localize("UI_RANDOM_OUTFIT"));
            craftedCountObject.SetActive(hasSelectedOutfit);
            randomOnlyOutfitInfoObject.SetActive(!hasSelectedOutfit);
            craftedCountText.SetText(L10nManager.Localize("UI_TOTAL_CRAFT_COUNT_FORMAT",
                hasSelectedOutfit &&
                _craftCountDict.TryGetValue(iconId, out var count)
                    ? count.ToCurrencyNotation()
                    : 0));

            var tableSheets = TableSheets.Instance;
            var relationshipRow = tableSheets.CustomEquipmentCraftRelationshipSheet
                .OrderedList.Last(row => row.Relationship <= ReactiveAvatarState.Relationship);
            _selectedItemId = relationshipRow.GetItemId(_selectedSubType!.Value);
            var equipmentItemSheet = tableSheets.EquipmentItemSheet;
            var equipmentRow = equipmentItemSheet[_selectedItemId];
            var customEquipmentCraftRecipeRow =
                tableSheets.CustomEquipmentCraftRecipeSheet.Values.First(r =>
                    r.ItemSubType == _selectedSubType);

            statView.Set(equipmentRow, relationshipRow, customEquipmentCraftRecipeRow, iconId);
            var viewSpinePreview =
                _selectedSubType is ItemSubType.Armor or ItemSubType.Weapon;
            selectedImageView.SetActive(!viewSpinePreview);
            selectedSpineView.SetActive(viewSpinePreview);
            if (hasSelectedOutfit)
            {
                selectedOutfitImage.overrideSprite =
                    SpriteHelper.GetItemIcon(iconId);
                if (viewSpinePreview)
                {
                    SetCharacter(equipmentRow, iconId);
                }
            }
            else
            {
                Action<int> routine = viewSpinePreview
                    ? id => SetCharacter(equipmentRow, id)
                    : id => selectedOutfitImage.overrideSprite = SpriteHelper.GetItemIcon(id);
                // 얻을 수 있는 외형 리스트를 가져온 뒤 랜덤으로 섞어서 번갈아가며 보여줍니다.
                var outfitIconIds = tableSheets.CustomEquipmentCraftIconSheet.Values
                    .Where(row => row.ItemSubType == _selectedSubType
                        && row.RequiredRelationship <= ReactiveAvatarState.Relationship)
                    .Select(row => row.IconId)
                    .OrderBy(_ => Random.value)
                    .ToList();
                _outfitAnimationDisposable = outfitIconIds
                    .ObservableIntervalLoopingList(.5f)
                    .Subscribe(index => routine(index));
            }

            var (_, materialCosts) = CustomCraftHelper.CalculateCraftCost(
                iconId,
                (int)ReactiveAvatarState.Relationship,
                tableSheets.MaterialItemSheet,
                customEquipmentCraftRecipeRow,
                relationshipRow,
                States.Instance.GameConfigState.CustomEquipmentCraftIconCostMultiplier
            );

            var additionalCost = CustomCraftHelper.CalculateAdditionalCost((int) ReactiveAvatarState.Relationship,
                tableSheets.CustomEquipmentCraftRelationshipSheet);

            SetCostAndMaterial(materialCosts.Select(pair =>
                    new EquipmentItemSubRecipeSheet.MaterialInfo(pair.Key, pair.Value))
                .ToList(),
                randomOnly,
                additionalCost);
        }

        private void SetCostAndMaterial(List<EquipmentItemSubRecipeSheet.MaterialInfo> materials, bool randomOnly, (BigInteger, IDictionary<int, int>)? additionalCost = null)
        {
            requiredItemRecipeView.SetData(
                materials,
                true);
            var ncgCost = 0L;
            if (additionalCost != null)
            {
                ncgCost = (long)additionalCost.Value.Item1;
                var costs = new List<ConditionalCostButton.CostParam>
                    {new(CostType.NCG, ncgCost)};
                costs.AddRange(additionalCost.Value.Item2
                    .Select(cost =>
                        new ConditionalCostButton.CostParam((CostType) cost.Key, cost.Value)));
                conditionalCostButton.SetCost(costs);
            }
            else
            {
                conditionalCostButton.SetCost(CostType.NCG, 0);
            }

            conditionalCostButton.SetCondition(() => !randomOnly);
            _submittableState = CheckSubmittableState(
                ncgCost,
                materials,
                _selectedOutfit.IconRow.Value?.RequiredRelationship ?? 0,
                randomOnly);
            conditionalCostButton.Interactable = _submittableState is SubmittableState.Able or SubmittableState.RandomOnly;
            conditionalCostButton.UpdateObjects();
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            _selectedOutfit?.Selected.SetValueAndForceNotify(false);
            _selectedOutfit = null;
            _outfitAnimationDisposable?.Dispose();
            _outfitAnimationDisposable = null;
            _selectedSubType = null;
            _disposables.DisposeAllAndClear();
        }

        private static SubmittableState CheckSubmittableState(
            long ncgAmount,
            List<EquipmentItemSubRecipeSheet.MaterialInfo> materials,
            int requiredRelationship,
            bool randomOnly)
        {
            if (LoadingHelper.CustomEquipmentCraft.Value)
            {
                return SubmittableState.WaitRenderingAction;
            }

            if (randomOnly)
            {
                return SubmittableState.RandomOnly;
            }

            if (ReactiveAvatarState.Relationship < requiredRelationship)
            {
                return SubmittableState.InsufficientRelationship;
            }

            if (!Find<CombinationSlotsPopup>().TryGetEmptyCombinationSlot(out _))
            {
                return SubmittableState.FullSlot;
            }

            if (States.Instance.GoldBalanceState.Gold.MajorUnit < ncgAmount)
            {
                return SubmittableState.InsufficientBalance;
            }

            var inventory = States.Instance.CurrentAvatarState.inventory;
            foreach (var material in materials)
            {
                if (!TableSheets.Instance.MaterialItemSheet.TryGetValue(material.Id, out var row))
                {
                    continue;
                }

                var itemCount = inventory.TryGetFungibleItems(row.ItemId, out var outFungibleItems)
                    ? outFungibleItems.Sum(e => e.count)
                    : 0;

                if (material.Count > itemCount)
                {
                    return SubmittableState.InsufficientMaterial;
                }
            }

            return SubmittableState.Able;
        }

        private void SetCharacter(ItemSheet.Row equipmentRow, int iconId)
        {
            var game = Game.Game.instance;
            var (equipments, costumes) = game.States.GetEquippedItems(BattleType.Adventure);
            costumes.Clear();
            var maxLevel = game.TableSheets.EnhancementCostSheetV3.Values
                .Where(row =>
                    row.ItemSubType == equipmentRow.ItemSubType &&
                    row.Grade == equipmentRow.Grade)
                .Max(row => row.Level);

            var previewItem = (Equipment)ItemFactory.CreateItemUsable(
                equipmentRow, Guid.NewGuid(), 0L, maxLevel);
            previewItem.IconId = iconId;

            equipments.RemoveAll(e =>
                e.ItemSubType == equipmentRow.ItemSubType ||
                e.ItemSubType == ItemSubType.Aura ||
                e.ItemSubType == ItemSubType.FullCostume ||
                e.ItemSubType == ItemSubType.Title);
            equipments.Add(previewItem);

            var avatarState = game.States.CurrentAvatarState;
            game.Lobby.FriendCharacter.Set(avatarState, costumes, equipments);
        }

        private IEnumerator CoCombineNPCAnimation(
            Equipment itemBase)
        {
            var loadingScreen = Find<CustomCraftLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SpeechBubbleWithItem.SetItemMaterial(new Item(itemBase));
            Push();
            yield return new WaitForSeconds(.5f);
            loadingScreen.SpeechBubbleWithItem.Show();
            loadingScreen.AnimateNPC();
        }

        private void ShowRelationshipInfoPopup(long relationship)
        {
            // 맨 처음엔 보여줄 필요가 없다.
            if (relationship == 0)
            {
                return;
            }

            // 이미 보여준 경우엔 안보여줘도 됨
            var key = string.Format(RelationshipInfoKey, relationship);
            if (PlayerPrefs.HasKey(key))
            {
                return;
            }

            // 이번에 제작하면 친밀도 구간이 넘어갈때!
            if (TableSheets.Instance.CustomEquipmentCraftRelationshipSheet.Values.Any(row => row.Relationship + 1 == relationship))
            {
                relationshipInfoPopupObject.SetActive(true);
                PlayerPrefs.SetString(key, string.Empty);
            }
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionShowInfo()
        {
            OnOutfitSelected(new CustomOutfit(null));
            PlayerPrefs.SetInt(TutorialKey, 1);
        }
    }
}
