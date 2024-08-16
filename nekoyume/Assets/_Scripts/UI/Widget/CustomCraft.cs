using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Nekoyume.Action.CustomEquipmentCraft;
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

        private CustomOutfit _selectedOutfit;

        private ItemSubType _selectedSubType;

        private List<IDisposable> _disposables = new();
        private IDisposable _outfitAnimationDisposable;

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
                .Subscribe(SetRelationshipView)
                .AddTo(gameObject);

            outfitScroll.OnClick.Subscribe(OnOutfitSelected).AddTo(gameObject);
            conditionalCostButton.OnSubmitSubject.Subscribe(_ => OnClickSubmitButton())
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            _selectedOutfit = null;
            notSelected.SetActive(true);
            selectedView.SetActive(false);
            SetRelationshipView(ReactiveAvatarState.Relationship);
            OnItemSubtypeSelected(ItemSubType.Weapon);
            ReactiveAvatarState.Inventory
                .Where(_ => _selectedOutfit != null)
                .Subscribe(_ =>
                {
                    OnOutfitSelected(_selectedOutfit);
                }).AddTo(_disposables);
        }

        /// <summary>
        /// 숙련도의 상태를 표시하는 View update 코드이다.
        /// State를 보여주는 기능으로, ActionRenderHandler나 ReactiveAvatarState를 반영해야 한다.
        /// </summary>
        /// <param name="relationship"></param>
        private void SetRelationshipView(long relationship)
        {
            relationshipText.SetText(relationship.ToString());
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

        private void OnClickSubmitButton()
        {
            if (Find<CombinationSlotsPopup>().TryGetEmptyCombinationSlot(out var slotIndex))
            {
                var recipe = TableSheets.Instance.CustomEquipmentCraftRecipeSheet.Values.First(r =>
                    r.ItemSubType == _selectedSubType);
                Find<CombinationSlotsPopup>().OnSendCombinationAction(
                    slotIndex,
                    recipe.RequiredBlock,
                    itemUsable: ItemFactory.CreateItemUsable(
                        TableSheets.Instance.EquipmentItemSheet[TableSheets.Instance
                            .CustomEquipmentCraftRelationshipSheet
                            .OrderedList
                            .First(row => row.Relationship >= ReactiveAvatarState.Relationship)
                            .GetItemId(_selectedSubType)]
                        , Guid.NewGuid(),
                        recipe.RequiredBlock));
                ActionManager.Instance.CustomEquipmentCraft(slotIndex,
                        recipe.Id,
                        _selectedOutfit.IconRow.Value?.IconId ?? CustomEquipmentCraft.RandomIconId)
                    .Subscribe();
                OnOutfitSelected(_selectedOutfit);
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
            outfitNameText.SetText(!selectRandomOutfit
                ? L10nManager.LocalizeItemName(_selectedOutfit.IconRow.Value.IconId)
                : "Random");
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
                        SpriteHelper.GetItemIcon(_selectedOutfit.IconRow.Value.IconId);
                    if (viewSpinePreview)
                    {
                        SetCharacter(equipmentRow, _selectedOutfit.IconRow.Value.IconId);
                    }
                }
                else
                {
                    Action<int> routine = viewSpinePreview
                        ? iconId => SetCharacter(equipmentRow, iconId)
                        : iconId =>
                            selectedOutfitImage.overrideSprite =
                                SpriteHelper.GetItemIcon(iconId);
                    var outfitIconIds = TableSheets.Instance.CustomEquipmentCraftIconSheet.Values
                        .Where(row =>
                            row.ItemSubType == _selectedSubType && row.RequiredRelationship <=
                            ReactiveAvatarState.Relationship)
                        .Select(row => row.IconId).ToList();
                    _outfitAnimationDisposable = outfitIconIds.ObservableIntervalLoopingList(.5f)
                        .Subscribe(index => routine(index));
                }
            }

            var recipeRow = TableSheets.Instance.CustomEquipmentCraftRecipeSheet.Values.First(r =>
                r.ItemSubType == _selectedSubType);
            var (ncgCost, materialCosts) = CustomCraftHelper.CalculateCraftCost(
                _selectedOutfit.IconRow.Value?.IconId ?? 0,
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
            conditionalCostButton.SetCondition(() => !_selectedOutfit.RandomOnly.Value);
            conditionalCostButton.Interactable = CheckSubmittable(
                ncgCost,
                materialCosts,
                _selectedOutfit.IconRow.Value?.RequiredRelationship ?? 0);
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

        private bool CheckSubmittable(BigInteger ncgAmount, IDictionary<int,int> materials, int requiredRelationship)
        {
            if (ReactiveAvatarState.Relationship < requiredRelationship)
            {
                return false;
            }

            if (!Find<CombinationSlotsPopup>().TryGetEmptyCombinationSlot(out _))
            {
                return false;
            }

            if (States.Instance.GoldBalanceState.Gold.MajorUnit < ncgAmount)
            {
                return false;
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
                    return false;
                }
            }

            return true;
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
    }
}
