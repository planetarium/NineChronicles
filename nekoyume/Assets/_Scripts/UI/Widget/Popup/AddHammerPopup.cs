using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class AddHammerPopup : PopupWidget
    {
        // Todo : Enhancement에 있는거랑 합쳐서 다른 class 로 뺴기
        [Serializable]
        public struct LevelTextGroup
        {
            public TextMeshProUGUI currentLevelText;
            public TextMeshProUGUI targetLevelText;
            public TextMeshProUGUI levelProgressText;
        }

        [Serializable]
        public struct HammerInformation
        {
            public VanillaItemView itemView;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI countText;
        }

        [Serializable]
        public struct HammerCountEditable
        {
            public TMP_InputField inputField;
            public Button maxButton;
            public Button resetButton;
            public Button[] addButtons;
        }

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private UpgradeEquipmentSlot baseSlot;

        [SerializeField]
        private LevelTextGroup levelTextGroup;

        [SerializeField]
        private EnhancementExpSlider enhancementExpSlider;

        [SerializeField]
        private HammerInformation hammerInformation;

        [SerializeField]
        private HammerCountEditable hammerCountEditable;

        [SerializeField]
        private ConditionalButton applyButton;

        private readonly Subject<int> _onChangeCount = new();
        private readonly List<IDisposable> _disposables = new();

        private Equipment _baseItem;
        private long _baseExp;
        private CountEditableItem _hammerItem;
        private Action<int> _onApply;

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(CloseWidget.Invoke);

            // Hammer Count Editable
            hammerCountEditable.inputField.onEndEdit.AsObservable().Subscribe(text =>
            {
                if (string.IsNullOrEmpty(text))
                {
                    _hammerItem.Count.Value = 0;
                }
                else
                {
                    var minCount = _hammerItem.MinCount.Value;
                    var maxCount = _hammerItem.MaxCount.Value;
                    var count = int.Parse(text);
                    _hammerItem.Count.Value = Mathf.Clamp(count, minCount, maxCount);
                }
            }).AddTo(gameObject);
            hammerCountEditable.maxButton.OnClickAsObservable().Subscribe(_ => { _hammerItem.Count.Value = _hammerItem.MaxCount.Value; }).AddTo(gameObject);
            hammerCountEditable.resetButton.OnClickAsObservable().Subscribe(_ => { _hammerItem.Count.Value = 0; }).AddTo(gameObject);
            for (var i = 0; i < hammerCountEditable.addButtons.Length; i++)
            {
                var digit = i;
                // add count : 10 ^ digit (1, 10, 100, 1000, ...)
                hammerCountEditable.addButtons[i].OnClickAsObservable().Subscribe(_ =>
                {
                    var maxCount = _hammerItem.MaxCount.Value;
                    var count = int.Parse(hammerCountEditable.inputField.text) +
                        (int)Mathf.Pow(10, digit);
                    _hammerItem.Count.Value = Mathf.Min(count, maxCount);
                }).AddTo(gameObject);
            }

            applyButton.OnSubmitSubject.Subscribe(_ =>
            {
                _onApply?.Invoke(_hammerItem.Count.Value);
                Close();
            }).AddTo(gameObject);

            _onChangeCount.Subscribe(count =>
            {
                hammerCountEditable.inputField.text = count.ToString();
                UpdateLevelTextGroup(count);
            }).AddTo(gameObject);
        }

        public void Show(
            Equipment baseModel,
            List<EnhancementInventoryItem> materialModels,
            EnhancementInventoryItem hammerItem,
            Action<int> onApply)
        {
            base.Show();

            _baseItem = baseModel;
            _baseExp = GetBaseExp(baseModel, materialModels, hammerItem.ItemBase);
            _hammerItem = new CountEditableItem(
                hammerItem.ItemBase,
                hammerItem.SelectedMaterialCount.Value,
                0,
                hammerItem.Count.Value);
            _onApply = onApply;

            baseSlot.AddMaterial(baseModel);

            hammerInformation.itemView.SetData(hammerItem.ItemBase);
            hammerInformation.nameText.text = hammerItem.ItemBase.GetLocalizedName();
            hammerInformation.countText.text = hammerItem.Count.Value.ToString();

            enhancementExpSlider.SetEquipment(baseModel, true);

            _disposables.DisposeAllAndClear();
            _hammerItem.Count
                .Subscribe(count => _onChangeCount.OnNext(count))
                .AddTo(_disposables);
        }

        private static long GetBaseExp(
            Equipment baseItem,
            List<EnhancementInventoryItem> materialModels,
            ItemBase ignoreItem)
        {
            var equipmentItemSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
            var enhancementCostSheet = Game.Game.instance.TableSheets.EnhancementCostSheetV3;

            var baseItemExp = baseItem.GetRealExp(
                equipmentItemSheet,
                enhancementCostSheet);
            var materialItemsExp = materialModels.Sum(inventoryItem =>
            {
                if (ItemEnhancement.HammerIds.Contains(inventoryItem.ItemBase.Id) && 
                    inventoryItem.ItemBase.Id != ignoreItem.Id)
                {
                    var hammerExp = enhancementCostSheet.GetHammerExp(
                        inventoryItem.ItemBase.Id);
                    return hammerExp * inventoryItem.SelectedMaterialCount.Value;
                }
                
                var equipment = inventoryItem.ItemBase as Equipment;
                if (equipment == null)
                {
                    return 0;
                }

                return equipment.GetRealExp(
                    equipmentItemSheet,
                    enhancementCostSheet);
            });

            return baseItemExp + materialItemsExp;
        }

        private void UpdateLevelTextGroup(int count)
        {
            var enhancementCostSheet = Game.Game.instance.TableSheets.EnhancementCostSheetV3;

            var baseEquipment = _baseItem;
            var baseItemCostRows = enhancementCostSheet.Values
                .Where(row => row.ItemSubType == baseEquipment.ItemSubType &&
                    row.Grade == baseEquipment.Grade).ToList();

            // Get Target Exp
            var materialsExp = enhancementCostSheet.GetHammerExp(
                _hammerItem.ItemBase.Value.Id);
            var targetExp = _baseExp + materialsExp * count;

            // Get Target Level
            EnhancementCostSheetV3.Row targetRow;
            try
            {
                targetRow = baseItemCostRows
                    .OrderByDescending(r => r.Exp)
                    .First(row => row.Exp <= targetExp);
            }
            catch
            {
                // It means that the target level is current level
                var currentRow =
                    baseItemCostRows.FirstOrDefault(row => row.Level == baseEquipment.level) ??
                    new EnhancementCostSheetV3.Row();
                targetRow = currentRow;
            }

            // Get Target Range Rows <- It might be unnecessary
            var targetRangeRows = baseItemCostRows
                .Where(row => row.Level >= baseEquipment.level &&
                    row.Level <= targetRow.Level + 1).ToList();
            if (baseEquipment.level == 0)
            {
                // current level row is empty
                targetRangeRows.Insert(0, new EnhancementCostSheetV3.Row());
            }

            if (targetRangeRows.Count >= 2)
            {
                enhancementExpSlider.SliderGageEffect(
                    targetExp,
                    targetRow.Level);
            }
            else
            {
                NcDebug.LogError($"[Enhancement] Failed Get TargetRangeRows : {baseEquipment.level} -> {targetRow.Level}");
            }

            // Update Level Text
            var maxLevel = ItemEnhancement.GetEquipmentMaxLevel(
                baseEquipment,
                enhancementCostSheet);
            levelTextGroup.currentLevelText.text = $"+{baseEquipment.level}";
            levelTextGroup.targetLevelText.text = $"+{targetRow.Level}";
            levelTextGroup.levelProgressText.text = $"Lv. {targetRow.Level}/{maxLevel}";
        }
    }
}
