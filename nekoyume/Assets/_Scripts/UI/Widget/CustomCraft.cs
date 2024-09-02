using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nekoyume.Action.CustomEquipmentCraft;
using Nekoyume.ApiClient;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.TableData;
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
        private Button relationshipHelpButton;

        [SerializeField]
        private Button skillListButton;

        [SerializeField]
        private TextMeshProUGUI outfitNameText;

        [SerializeField]
        private TextMeshProUGUI relationshipText;

        [SerializeField]
        private CustomOutfitScroll outfitScroll;

        [SerializeField]
        private TextMeshProUGUI baseStatText;

        [SerializeField]
        private TextMeshProUGUI expText;

        [SerializeField]
        private TextMeshProUGUI cpText;

        [SerializeField]
        private TextMeshProUGUI requiredBlockText;

        [SerializeField]
        private TextMeshProUGUI requiredLevelText;

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
        private TextMeshProUGUI maxMainStatText;
#endregion

        private CustomOutfit _selectedOutfit;
        private ItemSubType? _selectedSubType;
        private SubmittableState _submittableState;
        private int _selectedItemId;

        private readonly List<IDisposable> _disposables = new();
        private IDisposable _outfitAnimationDisposable;
        private readonly ConcurrentDictionary<int, long> _craftCountDict = new();

        public bool RequiredUpdateCraftCount { get; set; }

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
            OnItemSubtypeSelected(ItemSubType.Weapon);
            ReactiveAvatarState.Inventory
                .Where(_ => _selectedOutfit != null)
                .Subscribe(_ => OnOutfitSelected(_selectedOutfit))
                .AddTo(_disposables);
            ReactiveAvatarState.ObservableRelationship
                .Subscribe(relationship => relationshipText.SetText(relationship.ToString()))
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

            _selectedSubType = type;
            _selectedOutfit = null;
            var scrollData = new List<CustomOutfit> {new(null)};
            scrollData.AddRange(TableSheets.Instance.CustomEquipmentCraftIconSheet.Values
                .Where(row => row.ItemSubType == _selectedSubType)
                .Select(r => new CustomOutfit(r))
                .OrderBy(r => r.IconRow.Value.RequiredRelationship)
                .ThenBy(r => r.IconRow.Value.RandomOnly));
            outfitScroll.UpdateData(scrollData);
        }

        private void OnSubmitCraftButton()
        {
            var combinationSlotsPopup = Find<CombinationSlotsPopup>();
            if (combinationSlotsPopup.TryGetEmptyCombinationSlot(out var slotIndex))
            {
                var recipe = TableSheets.Instance.CustomEquipmentCraftRecipeSheet.Values.First(r =>
                    r.ItemSubType == _selectedSubType);
                var item = ItemFactory.CreateItemUsable(
                    TableSheets.Instance.EquipmentItemSheet[_selectedItemId],
                    Guid.NewGuid(),
                    recipe.RequiredBlock);
                combinationSlotsPopup.OnSendCombinationAction(
                    slotIndex,
                    recipe.RequiredBlock,
                    itemUsable: item);
                ActionManager.Instance.CustomEquipmentCraft(slotIndex,
                    recipe.Id,
                    _selectedOutfit.IconRow.Value?.IconId ?? CustomEquipmentCraft.RandomIconId)
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
                ? L10nManager.LocalizeItemName(iconId)
                : L10nManager.Localize("UI_RANDOM_OUTFIT"));
            craftedCountObject.SetActive(hasSelectedOutfit && !randomOnly);
            craftedCountText.SetText(L10nManager.Localize("UI_TOTAL_CRAFT_COUNT_FORMAT",
                hasSelectedOutfit &&
                _craftCountDict.TryGetValue(iconId, out var count)
                    ? count.ToCurrencyNotation()
                    : 0));

            var tableSheets = TableSheets.Instance;
            var relationshipRow = tableSheets.CustomEquipmentCraftRelationshipSheet
                .OrderedList.First(row => row.Relationship >= ReactiveAvatarState.Relationship);
            _selectedItemId = relationshipRow.GetItemId(_selectedSubType!.Value);
            var equipmentItemSheet = tableSheets.EquipmentItemSheet;
            var equipmentRow = equipmentItemSheet[_selectedItemId];
            var customEquipmentCraftRecipeRow =
                tableSheets.CustomEquipmentCraftRecipeSheet.Values.First(r =>
                    r.ItemSubType == _selectedSubType);

            baseStatText.SetText($"{equipmentRow.Stat.DecimalStatToString()}");
            expText.SetText($"EXP {equipmentRow.Exp?.ToCurrencyNotation()}");
            cpText.SetText($"CP: {relationshipRow.MinCp}-{relationshipRow.MaxCp}");
            maxMainStatText.SetText($"{equipmentRow.Stat.StatType} : MAX 100%");
            requiredBlockText.SetText($"{customEquipmentCraftRecipeRow.RequiredBlock}");
            requiredLevelText.SetText(
                $"Lv {tableSheets.ItemRequirementSheet[_selectedItemId].Level}");

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

            var (ncgCost, materialCosts) = CustomCraftHelper.CalculateCraftCost(
                iconId,
                tableSheets.MaterialItemSheet,
                customEquipmentCraftRecipeRow,
                relationshipRow,
                tableSheets.CustomEquipmentCraftCostSheet.Values
                    .FirstOrDefault(r => r.Relationship == ReactiveAvatarState.Relationship),
                States.Instance.GameConfigState.CustomEquipmentCraftIconCostMultiplier
            );

            SetCostAndMaterial(materialCosts.Select(pair =>
                    new EquipmentItemSubRecipeSheet.MaterialInfo(pair.Key, pair.Value))
                .ToList(), (long)ncgCost, randomOnly);
        }

        private void SetCostAndMaterial(List<EquipmentItemSubRecipeSheet.MaterialInfo> materials, long ncgCost, bool randomOnly)
        {
            requiredItemRecipeView.SetData(
                materials,
                true);
            conditionalCostButton.SetCost(CostType.NCG, ncgCost);
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
            ItemBase itemBase)
        {
            var loadingScreen = Find<CustomCraftLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SpeechBubbleWithItem.SetItemMaterial(new Item(itemBase));
            Push();
            yield return new WaitForSeconds(.5f);
            loadingScreen.AnimateNPC();
        }
    }
}
