using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action.CustomEquipmentCraft;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
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

        // [SerializeField]
        // private ConditionalCostButton conditionalCostButton;

        [SerializeField]
        private Button craftButton;

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

        private CustomOutfit _selectedOutfit;

        private ItemSubType _selectedSubType = ItemSubType.Weapon;

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
                Find<SummonSkillsPopup>().Show(TableSheets.Instance.SummonSheet.First);
                NcDebug.Log("skillListButton onclick");
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
            craftButton.onClick.AddListener(OnClickSubmitButton);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            _selectedOutfit = null;
            SetRelationshipView(ReactiveAvatarState.Relationship);
            OnItemSubtypeSelected(ItemSubType.Weapon);
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
            _selectedSubType = type;
            var scrollData = new List<CustomOutfit> {new(null)};
            scrollData.AddRange(TableSheets.Instance.CustomEquipmentCraftIconSheet.Values
                .Where(row => row.ItemSubType == _selectedSubType)
                .Select(r => new CustomOutfit(r)));
            outfitScroll.UpdateData(scrollData);
        }

        private void OnClickSubmitButton()
        {
            if (_selectedOutfit is null)
            {
                OneLineSystem.Push(MailType.System, "select outfit", NotificationCell.NotificationType.Information);
                return;
            }

            if (Find<CombinationSlotsPopup>().TryGetEmptyCombinationSlot(out var slotIndex))
            {
                // TODO: 전부 다 CustomEquipmentCraft 관련 sheet에서 가져오게 바꿔야함
                ActionManager.Instance.CustomEquipmentCraft(slotIndex,
                        TableSheets.Instance.CustomEquipmentCraftRecipeSheet.Values.First(r =>
                            r.ItemSubType == _selectedSubType).Id,
                        _selectedOutfit.IconRow.Value?.IconId ?? CustomEquipmentCraft.RandomIconId)
                    .Subscribe();
            }
        }

        private void OnOutfitSelected(CustomOutfit outfit)
        {
            if (_selectedOutfit != null)
            {
                _selectedOutfit.Selected.Value = false;
            }

            _selectedOutfit = outfit;
            _selectedOutfit.Selected.Value = true;

            outfitNameText.SetText(_selectedOutfit.IconRow.Value is not null ? L10nManager.LocalizeItemName(_selectedOutfit.IconRow.Value.IconId) : "Random");
            var relationshipRow = TableSheets.Instance.CustomEquipmentCraftRelationshipSheet
                .OrderedList.First(row => row.Relationship >= ReactiveAvatarState.Relationship);
            var equipmentItemId = relationshipRow.GetItemId(_selectedSubType);
            var equipmentItemSheet = TableSheets.Instance.EquipmentItemSheet;
            if (equipmentItemSheet.TryGetValue(equipmentItemId, out var equipmentRow))
            {
                // TODO: 싹 다 시안에 맞춰서 표현 방식을 변경해야한다. 지금은 외형을 선택하면 시트에서 잘 가져오는지 보려고 했다.
                baseStatText.SetText($"{equipmentRow.Stat.DecimalStatToString()}");
                expText.SetText($"EXP {equipmentRow.Exp!.Value.ToCurrencyNotation()}");
                cpText.SetText($"CP: {relationshipRow.MinCp}~{relationshipRow.MaxCp}");
                requiredBlockText.SetText(
                    $"{TableSheets.Instance.CustomEquipmentCraftRecipeSheet.Values.First(r => r.ItemSubType == _selectedSubType).RequiredBlock}");
                requiredLevelText.SetText($"Lv {TableSheets.Instance.ItemRequirementSheet[equipmentRow.Id].Level}");
                selectedOutfitImage.overrideSprite =
                    SpriteHelper.GetItemIcon(_selectedOutfit.IconRow.Value?.IconId ?? 0);
            }

            List<EquipmentItemSubRecipeSheet.MaterialInfo> requiredMaterials = new();
            var recipeRow = TableSheets.Instance.CustomEquipmentCraftRecipeSheet.Values.First(r =>
                r.ItemSubType == _selectedSubType);
            requiredMaterials.Add(new EquipmentItemSubRecipeSheet.MaterialInfo(CustomEquipmentCraft.DrawingItemId, (int)Math.Floor(recipeRow.DrawingAmount * relationshipRow.CostMultiplier)));
            requiredMaterials.Add(new EquipmentItemSubRecipeSheet.MaterialInfo(CustomEquipmentCraft.DrawingToolItemId, (int) Math.Floor(
                recipeRow.DrawingToolAmount * relationshipRow.CostMultiplier *
                (_selectedOutfit.IconRow.Value != null
                    ? States.Instance.GameConfigState.CustomEquipmentCraftIconCostMultiplier
                    : 1))));
            var ncgCost = 0L;
            var additionalCostRow = TableSheets.Instance.CustomEquipmentCraftCostSheet.OrderedList
                .FirstOrDefault(row => row.Relationship == ReactiveAvatarState.Relationship);
            if (additionalCostRow is not null)
            {
                if (additionalCostRow.GoldAmount > 0)
                {
                    ncgCost = (long)additionalCostRow.GoldAmount;
                }

                requiredMaterials.AddRange(additionalCostRow.MaterialCosts.Select(materialCost =>
                    new EquipmentItemSubRecipeSheet.MaterialInfo(materialCost.ItemId,
                        materialCost.Amount)));
            }

            requiredItemRecipeView.SetData(requiredMaterials, true);
            craftButton.interactable = !_selectedOutfit.RandomOnly.Value;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            _selectedOutfit = null;
        }
    }
}
