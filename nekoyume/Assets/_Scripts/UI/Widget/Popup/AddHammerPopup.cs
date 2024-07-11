using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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

        private Equipment _baseModel;
        private CountEditableItem _materialModel; // temp

        protected override void Awake()
        {
            base.Awake();

            closeButton.onClick.AddListener(CloseWidget.Invoke);

            // Hammer Count Editable
            hammerCountEditable.inputField.onEndEdit.AsObservable().Subscribe(text =>
            {
                if (string.IsNullOrEmpty(text))
                {
                    _onChangeCount.OnNext(0);
                }
                else
                {
                    var maxCount = _materialModel.MaxCount.Value;
                    var count = int.Parse(text);
                    _onChangeCount.OnNext(Mathf.Clamp(count, 1, maxCount));
                }
            }).AddTo(gameObject);
            hammerCountEditable.maxButton.OnClickAsObservable().Subscribe(_ =>
            {
                var maxCount = _materialModel.MaxCount.Value;
                _onChangeCount.OnNext(maxCount);
            }).AddTo(gameObject);
            hammerCountEditable.resetButton.OnClickAsObservable().Subscribe(_ =>
            {
                _onChangeCount.OnNext(0);
            }).AddTo(gameObject);
            for (var i = 0; i < hammerCountEditable.addButtons.Length; i++)
            {
                var digit = i;
                // add count : 10 ^ digit (1, 10, 100, 1000, ...)
                hammerCountEditable.addButtons[i].OnClickAsObservable().Subscribe(_ =>
                {
                    var maxCount = _materialModel.MaxCount.Value;
                    var count = int.Parse(hammerCountEditable.inputField.text) +
                                (int)Mathf.Pow(10, digit);
                    _onChangeCount.OnNext(Mathf.Min(count, maxCount));
                }).AddTo(gameObject);
            }

            applyButton.OnSubmitSubject.Subscribe(_ =>
            {
                // Todo : Apply Enhancement
            }).AddTo(gameObject);
        }

        public void Show(
            Equipment baseModel,
            EnhancementInventoryItem materialModel)
        {
            var materialCount = 0; // todo : fill this value (materialItem.Count.Value)

            _baseModel = baseModel;
            _materialModel = new CountEditableItem(materialModel.ItemBase,
                0,
                0,
                materialCount);

            baseSlot.AddMaterial(baseModel);

            hammerInformation.itemView.SetData(materialModel.ItemBase);
            hammerInformation.nameText.text = materialModel.ItemBase.GetLocalizedName();
            hammerInformation.countText.text = materialCount.ToString();

            enhancementExpSlider.SetEquipment(baseModel);
        }

        // Todo : Rename to Update...
        private void OnChangeCount(int count)
        {
            var equipmentItemSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
            var enhancementCostSheet = Game.Game.instance.TableSheets.EnhancementCostSheetV3;

            var baseEquipment = _baseModel;
            var baseItemCostRows = enhancementCostSheet.Values
                .Where(row => row.ItemSubType == baseEquipment.ItemSubType &&
                              row.Grade == baseEquipment.Grade).ToList();

            // Get Target Exp
            var baseModelExp = baseEquipment.GetRealExp(
                equipmentItemSheet,
                enhancementCostSheet);
            var materialsExp = Equipment.GetHammerExp(
                _materialModel.ItemBase.Value.Id,
                enhancementCostSheet);
            var targetExp = baseModelExp + materialsExp;

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
