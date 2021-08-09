using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;
using System.Numerics;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine.UI;
using EquipmentInventory = Nekoyume.UI.Module.EquipmentInventory;

namespace Nekoyume.UI
{
    using UniRx;
    public class UpgradeEquipment : Widget
    {
        [SerializeField] private EquipmentInventory inventory;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private UpgradeEquipmentSlot baseSlot;
        [SerializeField] private UpgradeEquipmentSlot materialSlot;
        [SerializeField] private TextMeshProUGUI successRatioText;
        [SerializeField] private TextMeshProUGUI requiredBlockIndexText;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI currentLevelText;
        [SerializeField] private TextMeshProUGUI nextLevelText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private EnhancementOptionView mainStat;
        [SerializeField] private List<EnhancementOptionView> addStats;
        [SerializeField] private List<EnhancementOptionView> addSkills;

        private EnhancementCostSheetV2 _costSheet;
        private Equipment _baseItem;
        private Equipment _materialItem;
        private BigInteger _costNcg = 0;

        protected override void Awake()
        {
            base.Awake();
            upgradeButton.onClick.AddListener(Action);
            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<CombinationMain>().Show();
            });

            CloseWidget = () =>
            {
                Close(true);
                Find<CombinationMain>().Show();
            };
        }

        public override void Initialize()
        {
            base.Initialize();

            Clear();
            inventory.SharedModel.SelectedItemView.Subscribe(ShowItemInformationTooltip).AddTo(gameObject);
            inventory.SharedModel.DeselectItemView();
            inventory.SharedModel.State.Value = ItemSubType.Weapon;
            inventory.SharedModel.DimmedFunc.Value = DimFunc;
            inventory.SharedModel.EffectEnabledFunc.Value = Contains;

            _costSheet = Game.Game.instance.TableSheets.EnhancementCostSheetV2;
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            Clear();
            HelpPopup.HelpMe(100017, true);
        }

        private bool DimFunc(InventoryItem inventoryItem)
        {
            if (_baseItem is null && _materialItem is null)
                return false;

            var selectedItem = (Equipment)inventoryItem.ItemBase.Value;

            if (!(_baseItem is null))
            {
                if (CheckDim(selectedItem, _baseItem))
                {
                    return true;
                }
            }

            if (!(_materialItem is null))
            {
                if (CheckDim(selectedItem, _materialItem))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckDim(Equipment selectedItem, Equipment slotItem)
        {
            if (selectedItem.ItemId.Equals(slotItem.ItemId))
            {
                return true;
            }

            if (selectedItem.ItemSubType != slotItem.ItemSubType)
            {
                return true;
            }

            if (selectedItem.Grade != slotItem.Grade)
            {
                return true;
            }

            if (selectedItem.level != slotItem.level)
            {
                return true;
            }

            return false;
        }

        private bool Contains(InventoryItem inventoryItem)
        {
            var selectedItem = (Equipment)inventoryItem.ItemBase.Value;

            if (!(_baseItem is null))
            {
                if (selectedItem.ItemId.Equals(_baseItem.ItemId))
                {
                    return true;
                }
            }

            if (!(_materialItem is null))
            {
                if (selectedItem.ItemId.Equals(_materialItem.ItemId))
                {
                    return true;
                }
            }

            return false;
        }

        private void Action()
        {
            var baseGuid = _baseItem.ItemId;
            var materialGuid = _materialItem.ItemId;
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            var slots = Find<CombinationSlots>();
            if (!slots.TryGetEmptyCombinationSlot(out var slotIndex))
            {
                return;
            }

            var sheet = Game.Game.instance.TableSheets.EnhancementCostSheetV2;
            if (ItemEnhancement.TryGetRow(_baseItem, sheet, out var row))
            {
                slots.SetCaching(slotIndex, true, row.SuccessRequiredBlockIndex, _baseItem);
            }

            LocalLayerModifier.ModifyAgentGold(agentAddress, -_costNcg);
            LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, -GameConfig.EnhanceEquipmentCostAP);
            LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, -GameConfig.EnhanceEquipmentCostAP);
            LocalLayerModifier.RemoveItem(avatarAddress, _baseItem.TradableId, _baseItem.RequiredBlockIndex, 1);
            LocalLayerModifier.RemoveItem(avatarAddress, _materialItem.TradableId, _materialItem.RequiredBlockIndex, 1);

            Notification.Push(MailType.Workshop, L10nManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_START"));

            Game.Game.instance.ActionManager
                .ItemEnhancement(baseGuid, materialGuid, slotIndex)
                .Subscribe(_ => { }, e => ActionRenderHandler.BackToMain(false, e));

            StartCoroutine(CoCombineNPCAnimation(_baseItem, Clear));
        }

        private IEnumerator CoCombineNPCAnimation(ItemBase itemBase, System.Action action, bool isConsumable = false)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SetItemMaterial(new Item(itemBase), isConsumable);
            loadingScreen.SetCloseAction(action);
            Push();
            yield return new WaitForSeconds(.5f);
            loadingScreen.AnimateNPC();
        }

        private void ShowItemInformationTooltip(InventoryItemView view)
        {
            var tooltip = Find<ItemInformationTooltip>();
            if (view is null || view.RectTransform == tooltip.Target)
            {
                tooltip.Close();
                return;
            }

            tooltip.Show(
                view.RectTransform,
                view.Model,
                value => !view.Model?.Dimmed.Value ?? false,
                L10nManager.Localize("UI_COMBINATION_REGISTER_MATERIAL"),
                _ => StageMaterial(view),
                _ => inventory.SharedModel.DeselectItemView());
        }

        private void StageMaterial(InventoryItemView viewModel)
        {
            if (_baseItem is null)
            {
                _baseItem = (Equipment) viewModel.Model.ItemBase.Value;
                if (ItemEnhancement.TryGetRow(_baseItem, _costSheet, out var row))
                {
                    _costNcg = row.Cost;
                    UpdateInformation(row, _baseItem);
                }

                baseSlot.AddMaterial(viewModel.Model.ItemBase.Value,
                    () => // unstage material callback
                    {
                        inventory.ClearItemState(_baseItem);
                        _baseItem = null;
                        inventory.SharedModel.UpdateDimAndEffectAll();
                        upgradeButton.interactable = IsInteractableButton(_baseItem, _materialItem, _costNcg);
                        ClearInformation();
                    });
            }
            else
            {
                _materialItem = (Equipment) viewModel.Model.ItemBase.Value;
                materialSlot.AddMaterial(viewModel.Model.ItemBase.Value,
                    () => // unstage material callback
                    {
                        inventory.ClearItemState(_materialItem);
                        _materialItem = null;
                        inventory.SharedModel.UpdateDimAndEffectAll();
                        upgradeButton.interactable = IsInteractableButton(_baseItem, _materialItem, _costNcg);
                    });
            }

            upgradeButton.interactable = IsInteractableButton(_baseItem, _materialItem, _costNcg);
            inventory.SharedModel.UpdateDimAndEffectAll();
        }

        private void Clear()
        {
            baseSlot.RemoveMaterial();
            materialSlot.RemoveMaterial();
            ClearInformation();
        }

        private void UpdateInformation(EnhancementCostSheetV2.Row row, Equipment equipment)
        {
            ClearInformation();
            costText.text = row.Cost.ToString();
            costText.color = GetNcgColor(row.Cost);
            itemNameText.text = equipment.GetLocalizedName();
            currentLevelText.text = $"{equipment.level}";
            nextLevelText.text = $"{equipment.level + 1}";
            successRatioText.text =
                ((row.GreatSuccessRatio + row.SuccessRatio) * GameConfig.TenThousandths).ToString("P0");
            requiredBlockIndexText.text = $"{row.SuccessRequiredBlockIndex}+";

            var stats = equipment.StatsMap.GetStats().ToList();
            foreach (var stat in stats)
            {
                if (stat.StatType.Equals(equipment.UniqueStatType))
                {
                    var mainType = stat.StatType.ToString();
                    var mainValue = stat.ValueAsInt.ToString();
                    var mainAdd = (int)(stat.ValueAsInt * row.BaseStatGrowthMax  * GameConfig.TenThousandths);
                    mainStat.gameObject.SetActive(true);
                    mainStat.Set(mainType, mainValue, $"(<size=80%>max</size> +{mainAdd})");
                    break;
                }
            }

            if (equipment.GetOptionCount() > 0)
            {
                for (var i = 0; i < stats.Count; i++)
                {
                    var subType = stats[i].StatType.ToString();
                    var subValue = stats[i].AdditionalValueAsInt.ToString();
                    var subAddValue = (int)(stats[i].AdditionalValueAsInt * row.ExtraStatGrowthMax * GameConfig.TenThousandths);
                    var subAdd = stats[i].StatType == StatType.SPD
                        ? (subAddValue * 0.01m).ToString(CultureInfo.InvariantCulture)
                        : subAddValue.ToString(CultureInfo.InvariantCulture);
                    addStats[i].gameObject.SetActive(true);
                    addStats[i].Set(subType,
                        subValue,
                        $"(<size=80%>max</size> +{subAdd})");
                }
            }

            var skills = equipment.Skills;
            for (var i = 0; i < skills.Count; i++)
            {
                var name = skills[i].SkillRow.GetLocalizedName();
                var power = skills[i].Power.ToString();
                var chance = skills[i].Chance.ToString();
                var powerAdd = (int)(skills[i].Power * row.ExtraSkillDamageGrowthMax * GameConfig.TenThousandths);
                var chanceAdd = (int)(skills[i].Chance * row.ExtraSkillChanceGrowthMax * GameConfig.TenThousandths);
                addSkills[i].gameObject.SetActive(true);
                addSkills[i].Set(name,
                    $"{L10nManager.Localize("UI_SKILL_POWER")} : {power}",
                    $"(<size=80%>max</size> +{powerAdd})",
                    $"{L10nManager.Localize("UI_SKILL_CHANCE")} : {chance}",
                    $"(<size=80%>max</size> +{chanceAdd}%)");
            }
        }

        private void ClearInformation()
        {
            costText.text = "0";
            itemNameText.text = string.Empty;
            currentLevelText.text = string.Empty;
            nextLevelText.text = string.Empty;
            successRatioText.text = "0%";
            requiredBlockIndexText.text = "0";
            upgradeButton.interactable = false;

            mainStat.gameObject.SetActive(false);
            foreach (var stat in addStats)
            {
                stat.gameObject.SetActive(false);
            }

            foreach (var skill in addSkills)
            {
                skill.gameObject.SetActive(false);
            }
        }

        private bool IsInteractableButton(IItem item, IItem material, BigInteger cost)
        {
            if (item is null || material is null)
            {
                return false;
            }

            if (States.Instance.GoldBalanceState.Gold.MajorUnit < cost)
            {
                return false;
            }

            if (States.Instance.CurrentAvatarState.actionPoint < GameConfig.EnhanceEquipmentCostAP)
            {
                return false;
            }

            return Find<CombinationSlots>().TryGetEmptyCombinationSlot(out _);
        }

        private static Color GetNcgColor(BigInteger cost)
        {
            return States.Instance.GoldBalanceState.Gold.MajorUnit < cost
                ? Palette.GetColor(ColorType.TextDenial)
                : Color.white;
        }
    }
}
