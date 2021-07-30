using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;
using System.Numerics;
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
        [SerializeField] private EnhancementOptionView mainOptions;
        [SerializeField] private List<EnhancementOptionView> addOptions;

        private EnhancementCostSheetV2 _costSheet;
        private Equipment _baseItem;
        private Equipment _materialItem;
        private BigInteger CostNCG = 0;
        private int CostAP = 0;

        protected override void Awake()
        {
            base.Awake();
            upgradeButton.onClick.AddListener(ActionUpgradeItem);
            closeButton.onClick.AddListener(() => Close(true));
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

        private void ActionUpgradeItem()
        {
            var baseGuid = _baseItem.ItemId;
            var materialGuid = _materialItem.ItemId;
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var slotIndex = Find<Combination>().selectedIndex;

            LocalLayerModifier.ModifyAgentGold(agentAddress, CostNCG * -1);
            LocalLayerModifier.ModifyAvatarActionPoint(avatarAddress, -CostAP);
            LocalLayerModifier.RemoveItem(avatarAddress, _baseItem.TradableId, _baseItem.RequiredBlockIndex, 1);
            LocalLayerModifier.RemoveItem(avatarAddress, _materialItem.TradableId, _materialItem.RequiredBlockIndex, 1);

            // todo : 로컬레이어 대체할걸 만들어야됨
            // LocalLayerModifier.ModifyCombinationSlotItemEnhancement(baseGuid, materialGuid, slotIndex);
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
                if (TryGetRow(_baseItem, _costSheet, out var row))
                {
                    UpdateInformation(row, _baseItem);
                }

                baseSlot.AddMaterial(viewModel.Model.ItemBase.Value,
                    () => // unstage material callback
                    {
                        inventory.ClearItemState(_baseItem);
                        _baseItem = null;
                        inventory.SharedModel.UpdateDimAndEffectAll();
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
                    });
            }

            upgradeButton.enabled = !(_baseItem is null) && !(_materialItem is null);
            inventory.SharedModel.UpdateDimAndEffectAll();
        }

        private bool TryGetRow(Equipment equipment,
                               EnhancementCostSheetV2 sheet,
                               out EnhancementCostSheetV2.Row row)
        {
            var grade = equipment.Grade;
            var level = equipment.level + 1;
            row = sheet.OrderedList.FirstOrDefault(x => x.Grade == grade  && x.Level == level);
            return row != null;
        }

        private void Clear()
        {
            baseSlot.RemoveMaterial();
            materialSlot.RemoveMaterial();
            ClearInformation();
        }

        private void UpdateInformation(EnhancementCostSheetV2.Row row, Equipment equipment)
        {
            currentLevelText.text = $"{equipment.level}";
            nextLevelText.text = $"{equipment.level + 1}";
            successRatioText.text = $"{(row.GreatSuccessRatio + row.SuccessRatio) * 100}%";
            requiredBlockIndexText.text = $"{row.SuccessRequiredBlockIndex}+";
            itemNameText.text = equipment.GetLocalizedName();

            foreach (var addStat in addOptions)
            {
                addStat.gameObject.SetActive(false);
            }

            var stats = equipment.StatsMap.GetStats().ToList();
            foreach (var stat in stats)
            {
                if (stat.StatType.Equals(equipment.UniqueStatType))
                {
                    var mainType = stat.StatType.ToString();
                    var mainValue = stat.ValueAsInt.ToString();
                    var mainAdd = (int)(row.BaseStatGrowthMax * 100);
                    mainOptions.gameObject.SetActive(true);
                    mainOptions.Initialize(mainType, mainValue, $"MAX  +{mainAdd}%",
                        string.Empty, string.Empty);
                    break;
                }
            }

            if (equipment.GetOptionCount() > 0)
            {
                for (var i = 0; i < stats.Count; i++)
                {
                    var subType = stats[i].StatType.ToString();
                    var subValue = stats[i].AdditionalValueAsInt.ToString();
                    var subAdd = (int)(row.ExtraStatGrowthMax * 100);
                    addOptions[i].gameObject.SetActive(true);
                    addOptions[i].Initialize(subType, subValue, $"MAX  +{subAdd}%",
                        string.Empty, string.Empty);
                }
            }

            var skills = equipment.Skills;
            for (var i = 0; i < skills.Count; i++)
            {
                var name = skills[i].SkillRow.GetLocalizedName();
                var power = skills[i].Power.ToString();
                var chance = skills[i].Chance.ToString();
                var powerAdd = (int)(row.ExtraSkillDamageGrowthMax * 100);
                var chanceAdd = (int)(row.ExtraSkillChanceGrowthMax * 100);
                var options = addOptions[i + stats.Count];
                options.gameObject.SetActive(true);
                options.Initialize(name, power, $"MAX +{powerAdd}%",
                    chance, $"MAX  +{chanceAdd}%");
            }
        }

        private void ClearInformation()
        {
            currentLevelText.text = string.Empty;
            nextLevelText.text = string.Empty;
            successRatioText.text = "0%";
            requiredBlockIndexText.text = "0";
            mainOptions.gameObject.SetActive(false);
            foreach (var addStat in addOptions)
            {
                addStat.gameObject.SetActive(false);
            }
        }
    }
}
