#nullable enable

using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class SynthesisRegistrationPopup : PopupWidget
    {
#region SerializeField

        [SerializeField] private Button closeButton = null!;
        [SerializeField] private SynthesisInventory synthesisInventory = null!;
        [SerializeField] private EquipmentTooltip equipmentTooltip = null!;
        [SerializeField] private SynthesisItemView synthesisItemView = null!;

        [Header("Buttons")]
        [SerializeField] private ConditionalButton autoSelectButton = null!;
        [SerializeField] private ConditionalButton autoSelectAllButton = null!;
        [SerializeField] private ConditionalButton registrationButton = null!;

        [Header("Header")]
        [SerializeField] private TMP_Text numberSynthesisText = null!;
        [SerializeField] private TMP_Text typeText = null!;
        [SerializeField] private TMP_Text holdText = null!;

#endregion SerializeField

        private Action<IList<InventoryItem>>? _registerMaterials;

        private SynthesizeModel? _synthesizeModel;

#region MonoBehaviour

        protected override void Awake()
        {
            CheckNull();

            base.Awake();

            closeButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                CloseWidget.Invoke();
            });
            CloseWidget = () => { Close(); };

            autoSelectButton.OnSubmitSubject
                .Subscribe(_ => OnClickAutoSelectButton())
                .AddTo(gameObject);

            autoSelectAllButton.OnSubmitSubject
                .Subscribe(_ => OnClickAutoSelectAllButton())
                .AddTo(gameObject);

            registrationButton.OnSubmitSubject
                .Subscribe(_ => OnClickRegisterButton())
                .AddTo(gameObject);

            synthesisInventory.SetInventory(OnClickInventoryItem);
        }

#endregion MonoBehaviour

#region Button

        private void OnClickAutoSelectButton()
        {
            if (_synthesizeModel == null)
            {
                NcDebug.LogWarning("_synthesizeModel is null.");
                return;
            }

            var result = synthesisInventory.SelectAutoSelectItems(_synthesizeModel);
            if (!result)
            {
                NcDebug.LogWarning("Failed to select items.");
            }
        }

        private void OnClickAutoSelectAllButton()
        {
            if (_synthesizeModel == null)
            {
                NcDebug.LogWarning("_synthesizeModel is null.");
                return;
            }

            var result = synthesisInventory.SelectAutoSelectAllItems(_synthesizeModel);
            if (!result)
            {
                NcDebug.LogWarning("Failed to select all items.");
            }
        }

        private void OnClickRegisterButton()
        {
            foreach (var selectedItem in synthesisInventory.SelectedItems)
            {
                if (!selectedItem.Equipped.Value)
                {
                    continue;
                }

                var confirm = Find<IconAndButtonSystem>();
                confirm.ShowWithTwoButton(
                    "UI_WARNING", "UI_COLLECTION_REGISTRATION_CAUTION_PHRASE");
                confirm.ConfirmCallback = () => RegisterItem(synthesisInventory.SelectedItems);
                confirm.CancelCallback = () => confirm.Close();
                return;
            }

            RegisterItem(synthesisInventory.SelectedItems);
        }

        private void OnClickInventoryItem(InventoryItem item)
        {
            ShowItemTooltip(item);
        }

#endregion Button

#region Widget

        public void Show(
            SynthesizeModel model,
            Action<IList<InventoryItem>> registerAction,
            bool ignoreShowAnimation = false)
        {
            _synthesizeModel = model;
            _registerMaterials = registerAction;

            base.Show(ignoreShowAnimation);

            SetAutoSelectButtonText(model);
            SetHeaderText(model);

            synthesisInventory.Show(model);
        }

        private void SetAutoSelectButtonText(SynthesizeModel model)
        {
            autoSelectButton.SetText(L10nManager.Localize("UI_SYNTHESIZE_AUTO_SELECT", model.RequiredItemCount));
        }

        private void SetHeaderText(SynthesizeModel model)
        {
            numberSynthesisText.text = L10nManager.Localize("UI_NUMBER_SYNTHESIS", -1);

            var intGrade = (int)model.Grade;
            var gradeText = L10nManager.Localize($"UI_ITEM_GRADE_{intGrade}");
            typeText.text = L10nManager.Localize("UI_SYNTHESIZE_MATERIAL", gradeText);
            typeText.color = LocalizationExtensions.GetItemGradeColor(intGrade);

            var holdHeader = L10nManager.Localize("UI_SYNTHESIZE_HOLDS")!;
            var outputItemCount = model.InventoryItemCount / model.RequiredItemCount;
            holdText.text = $"{holdHeader}: {model.InventoryItemCount}/{model.RequiredItemCount} ({outputItemCount})";
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);
            _registerMaterials = null;
        }

#endregion Widget

        private void RegisterItem(IList<InventoryItem> items)
        {
            if (_registerMaterials == null)
            {
                NcDebug.LogError("_registerMaterials is null.");
                return;
            }

            _registerMaterials.Invoke(items);

            // TODO: 강화된 아이템 체크
            CloseWidget.Invoke();
        }

        private void ShowItemTooltip(InventoryItem item)
        {
            if (item.ItemBase is null)
            {
                return;
            }

            equipmentTooltip.Show(item, string.Empty, false, null);
            equipmentTooltip.OnEnterButtonArea(true);
        }

#region Helpers

        private void CheckNull()
        {
            if (closeButton == null)
            {
                throw new NullReferenceException("activeBackgroundObject is null");
            }

            if (registrationButton == null)
            {
                throw new NullReferenceException("registrationButton is null");
            }

            if (synthesisInventory == null)
            {
                throw new NullReferenceException("synthesisInventory is null");
            }

            if (equipmentTooltip == null)
            {
                throw new NullReferenceException("equipmentTooltip is null");
            }

            if (synthesisItemView == null)
            {
                throw new NullReferenceException("synthesisItemView is null");
            }
        }

#endregion Helpers
    }
}
