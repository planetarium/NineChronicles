using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
            FullSlot
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
#endregion

        private CustomOutfit _selectedOutfit;
        private ItemSubType _selectedSubType;
        private SubmittableState _submittableState;

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
                Find<CustomEquipmentSkillPopup>().Show(_selectedSubType);
            });
            foreach (var subTypeButton in subTypeButtons)
            {
                subTypeButton.toggleButton.onClickToggle.AddListener(() =>
                {
                    OnItemSubtypeSelected(subTypeButton.itemSubType);
                });
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            ReactiveAvatarState.ObservableRelationship
                .Where(_ => isActiveAndEnabled)
                .Subscribe(relationship => relationshipText.SetText(relationship.ToString()))
                .AddTo(gameObject);

            outfitScroll.OnClick
                .Subscribe(OnOutfitSelected)
                .AddTo(gameObject);
            conditionalCostButton.OnSubmitSubject
                .Subscribe(_ => OnSubmitCraftButton())
                .AddTo(gameObject);
                .AddTo(gameObject);
            UpdateCraftCount();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            _selectedOutfit = null;
            notSelected.SetActive(true);
            selectedView.SetActive(false);
            relationshipText.SetText(ReactiveAvatarState.Relationship.ToString());
            OnItemSubtypeSelected(ItemSubType.Weapon);
            ReactiveAvatarState.Inventory
                .Where(_ => _selectedOutfit != null)
                .Subscribe(_ =>
                {
                    OnOutfitSelected(_selectedOutfit);
                }).AddTo(_disposables);

            if (RequiredUpdateCraftCount)
            {
                UpdateCraftCount();
                RequiredUpdateCraftCount = false;
            }

            base.Show(ignoreShowAnimation);
        }

        private void UpdateCraftCount()
        {
            UniTask.Run(async () =>
            {
                if (ApiClients.Instance.WorldBossClient.IsInitialized)
                {
                    foreach (var customEquipmentCraftIconCountResponse in
                        (await CustomCraftQuery.GetCustomEquipmentCraftIconCountAsync(
                            ApiClients.Instance.WorldBossClient))
                        .customEquipmentCraftIconCount)
                    {
                        _craftCountDict[customEquipmentCraftIconCountResponse.iconId] =
                            customEquipmentCraftIconCountResponse.count;
                    }
                }
            }).Forget();
        }

        /// <summary>
        /// 어떤 종류의 장비를 만들지 ItemSubType을 선택하면 실행될 콜백, View 업데이트를 한다
        /// </summary>
        /// <param name="type"></param>
        private void OnItemSubtypeSelected(ItemSubType type)
        {
            if (_selectedSubType == type)
            {
                return;
            }

            _selectedSubType = type;
            var scrollData = new List<CustomOutfit> {new(null)};
            scrollData.AddRange(TableSheets.Instance.CustomEquipmentCraftIconSheet.Values
                .Where(row => row.ItemSubType == _selectedSubType)
                .Select(r => new CustomOutfit(r))
                .OrderBy(r => r.IconRow.Value.RequiredRelationship)
                .ThenBy(r => r.IconRow.Value.RandomOnly));
            outfitScroll.UpdateData(scrollData);
            _selectedOutfit = null;
            notSelected.SetActive(true);
            selectedView.SetActive(false);
        }

        private void OnSubmitCraftButton()
        {
            if (Find<CombinationSlotsPopup>().TryGetEmptyCombinationSlot(out var slotIndex))
            {
                var recipe = TableSheets.Instance.CustomEquipmentCraftRecipeSheet.Values.First(r =>
                    r.ItemSubType == _selectedSubType);
                var item = ItemFactory.CreateItemUsable(
                    TableSheets.Instance.EquipmentItemSheet[TableSheets.Instance
                        .CustomEquipmentCraftRelationshipSheet
                        .OrderedList
                        .First(row => row.Relationship >= ReactiveAvatarState.Relationship)
                        .GetItemId(_selectedSubType)]
                    , Guid.NewGuid(),
                    recipe.RequiredBlock);
                Find<CombinationSlotsPopup>().OnSendCombinationAction(
                    slotIndex,
                    recipe.RequiredBlock,
                    itemUsable: item);
                ActionManager.Instance.CustomEquipmentCraft(slotIndex,
                        recipe.Id,
                        _selectedOutfit.IconRow.Value?.IconId ?? CustomEquipmentCraft.RandomIconId)
                    .Subscribe();
                OnOutfitSelected(_selectedOutfit);
                StartCoroutine(CoCombineNPCAnimation(item, recipe.RequiredBlock));
            }
            else
            {
                // todo: 뭔진 몰라도 not interactable한데 interact를 한게 문제니까 일단...
                OneLineSystem.Push(MailType.System, "somethings wrong",
                    NotificationCell.NotificationType.Information);
            }
        }

        private void OnOutfitSelected(CustomOutfit outfit)
        {
            _outfitAnimationDisposable?.Dispose();
            if (_selectedOutfit != null)
            {
                _selectedOutfit.Selected.Value = false;
            }

            _selectedOutfit = outfit;
            _selectedOutfit.Selected.Value = true;

            notSelected.SetActive(false);
            selectedView.SetActive(true);
            selectedImageView.SetActive(true);
            selectedSpineView.SetActive(false);
            // 외형 랜덤 선택
            var selectRandomOutfit = _selectedOutfit.IconRow.Value == null;
            var randomOnly = _selectedOutfit.RandomOnly.Value;
            var iconId = _selectedOutfit.IconRow.Value?.IconId ?? CustomEquipmentCraft.RandomIconId;
            outfitNameText.SetText(!selectRandomOutfit
                ? L10nManager.LocalizeItemName(iconId)
                : L10nManager.Localize("UI_RANDOM_OUTFIT"));
            craftedCountObject.SetActive(!selectRandomOutfit && !randomOnly);
            if (!selectRandomOutfit &&
                _craftCountDict.TryGetValue(iconId, out var count))
            {
                craftedCountText.SetText(L10nManager.Localize("UI_TOTAL_CRAFT_COUNT_FORMAT", count.ToCurrencyNotation()));
            }
            else
            {
                craftedCountText.SetText(L10nManager.Localize("UI_TOTAL_CRAFT_COUNT_FORMAT", 0));
            }

            var relationshipRow = TableSheets.Instance.CustomEquipmentCraftRelationshipSheet
                .OrderedList.First(row => row.Relationship >= ReactiveAvatarState.Relationship);
            var equipmentItemId = relationshipRow.GetItemId(_selectedSubType);
            var equipmentItemSheet = TableSheets.Instance.EquipmentItemSheet;
            if (equipmentItemSheet.TryGetValue(equipmentItemId, out var equipmentRow))
            {
                baseStatText.SetText($"{equipmentRow.Stat.DecimalStatToString()}");
                expText.SetText($"EXP {equipmentRow.Exp!.Value.ToCurrencyNotation()}");
                cpText.SetText($"CP: {relationshipRow.MinCp}~{relationshipRow.MaxCp}");
                requiredBlockText.SetText(
                    $"{TableSheets.Instance.CustomEquipmentCraftRecipeSheet.Values.First(r => r.ItemSubType == _selectedSubType).RequiredBlock}");
                requiredLevelText.SetText(
                    $"Lv {TableSheets.Instance.ItemRequirementSheet[equipmentRow.Id].Level}");

                var viewSpinePreview =
                    equipmentRow.ItemSubType is ItemSubType.Armor or ItemSubType.Weapon;
                selectedImageView.SetActive(!viewSpinePreview);
                selectedSpineView.SetActive(viewSpinePreview);
                if (!selectRandomOutfit)
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
                    var outfitIconIds = TableSheets.Instance.CustomEquipmentCraftIconSheet.Values
                        .Where(row => row.ItemSubType == _selectedSubType
                            && row.RequiredRelationship <= ReactiveAvatarState.Relationship)
                        .Select(row => row.IconId)
                        .OrderBy(_ => Random.value)
                        .ToList();
                    _outfitAnimationDisposable = outfitIconIds
                        .ObservableIntervalLoopingList(.5f)
                        .Subscribe(index => routine(index));
                }
            }

            var recipeRow = TableSheets.Instance.CustomEquipmentCraftRecipeSheet.Values.First(r =>
                r.ItemSubType == _selectedSubType);
            var (ncgCost, materialCosts) = CustomCraftHelper.CalculateCraftCost(
                iconId,
                TableSheets.Instance.MaterialItemSheet,
                recipeRow,
                relationshipRow,
                TableSheets.Instance.CustomEquipmentCraftCostSheet.Values
                    .FirstOrDefault(r => r.Relationship == ReactiveAvatarState.Relationship),
                States.Instance.GameConfigState.CustomEquipmentCraftIconCostMultiplier
            );

            requiredItemRecipeView.SetData(
                materialCosts.Select(pair =>
                        new EquipmentItemSubRecipeSheet.MaterialInfo(pair.Key, pair.Value))
                    .ToList(),
                true);
            conditionalCostButton.SetCost(CostType.NCG, (long)ncgCost);

            conditionalCostButton.SetCondition(() => !randomOnly);
            _submittableState = CheckSubmittableState(
                ncgCost,
                materialCosts,
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
            _disposables.DisposeAllAndClear();
        }

        private static SubmittableState CheckSubmittableState(
            BigInteger ncgAmount,
            IDictionary<int, int> materials,
            int requiredRelationship,
            bool randomOnly)
        {
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
                if (!TableSheets.Instance.MaterialItemSheet.TryGetValue(material.Key, out var row))
                {
                    continue;
                }

                var itemCount = inventory.TryGetFungibleItems(row.ItemId, out var outFungibleItems)
                    ? outFungibleItems.Sum(e => e.count)
                    : 0;

                if (material.Value > itemCount)
                {
                    return SubmittableState.InsufficientMaterial;
                }
            }

            return SubmittableState.Able;
        }

        private void SetCharacter(EquipmentItemSheet.Row equipmentRow, int iconId)
        {
            var game = Game.Game.instance;
            var (equipments, costumes) = game.States.GetEquippedItems(BattleType.Adventure);

            costumes.Clear();
            if (equipmentRow is not null)
            {
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
                    e.ItemSubType == ItemSubType.FullCostume);
                equipments.Add(previewItem);
            }

            var avatarState = game.States.CurrentAvatarState;
            game.Lobby.FriendCharacter.Set(avatarState, costumes, equipments);
            game.Lobby.FriendCharacter.Animator.Attack();
        }

        private IEnumerator CoCombineNPCAnimation(
            ItemBase itemBase,
            long blockIndex)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SpeechBubbleWithItem.SetItemMaterial(new Item(itemBase));
            Push();
            yield return new WaitForSeconds(.5f);

            var format = L10nManager.Localize("UI_COST_BLOCK");
            var quote = string.Format(format, blockIndex);
            loadingScreen.AnimateNPC(CombinationLoadingScreen.SpeechBubbleItemType.Equipment, quote);
        }
    }
}
