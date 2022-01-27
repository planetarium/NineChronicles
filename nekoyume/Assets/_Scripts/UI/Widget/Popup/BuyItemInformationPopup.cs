using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class BuyItemInformationPopup : PopupWidget
    {
        public VerticalLayoutGroup verticalLayoutGroup;
        public TextMeshProUGUI itemNameText;
        public CombinationItemInformation itemInformation;
        public TextMeshProUGUI materialText;
        public SimpleCountableItemView[] materialItems;
        public Button submitButton;
        public GameObject materialView;
        public TouchHandler touchHandler;
        public Image consumableHeader;
        public Image equipmentHeader;
        public TextMeshProUGUI cpText;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        private Model.BuyItemInformationPopup Model { get; set; }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            submitButton.OnClickAsObservable().Subscribe(_ => Close()).AddTo(gameObject);
            touchHandler.OnClick.Subscribe(pointerEventData =>
            {
                if (!pointerEventData.pointerCurrentRaycast.gameObject.Equals(gameObject))
                    return;

                Close();
            }).AddTo(gameObject);

            CloseWidget = null;
            SubmitWidget = submitButton.onClick.Invoke;
        }

        protected override void OnDestroy()
        {
            Clear();
            base.OnDestroy();
        }

        #endregion

        public void Pop(Model.BuyItemInformationPopup data)
        {
            if (data is null)
            {
                return;
            }

            base.Show();
            SetData(data);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) verticalLayoutGroup.transform);
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);

            Model.OnClickSubmit.OnNext(Model);
            AudioController.PlayClick();
        }

        private void SetData(Model.BuyItemInformationPopup data)
        {
            if (data is null)
            {
                Clear();
                return;
            }

            _disposablesForModel.DisposeAllAndClear();
            Model = data;
            Model.itemInformation.Subscribe(itemInformation.SetData).AddTo(_disposablesForModel);

            UpdateView();
        }

        private void Clear()
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = null;

            UpdateView();
        }

        private void UpdateView()
        {
            if (Model is null)
            {
                itemNameText.text = L10nManager.Localize("UI_COMBINATION_ERROR");
                itemInformation.gameObject.SetActive(false);
                return;
            }

            var item = Model.itemInformation.Value.item.Value.ItemBase.Value;
            var isEquipment = item is Equipment;

            if (Model.isSuccess)
            {
                itemNameText.text = item.GetLocalizedName(false);
                itemInformation.gameObject.SetActive(true);
                AudioController.instance.PlaySfx(AudioController.SfxCode.Success);
            }
            else
            {
                itemNameText.text = L10nManager.Localize("UI_COMBINATION_FAIL");
                itemInformation.gameObject.SetActive(false);
                AudioController.instance.PlaySfx(AudioController.SfxCode.Failed);
            }

            if (Model.materialItems.Any())
            {
                materialText.gameObject.SetActive(true);
                materialView.SetActive(true);
                using (var e = Model.materialItems.GetEnumerator())
                {
                    foreach (var material in materialItems)
                    {
                        e.MoveNext();
                        if (e.Current is null)
                        {
                            material.Clear();
                            material.gameObject.SetActive(false);
                        }
                        else
                        {
                            var data = e.Current;
                            material.SetData(data);
                            material.gameObject.SetActive(true);
                        }
                    }
                }
            }
            else
            {
                materialText.gameObject.SetActive(false);
                materialView.SetActive(false);
            }

            consumableHeader.gameObject.SetActive(!isEquipment);
            equipmentHeader.gameObject.SetActive(isEquipment);
            if (isEquipment)
            {
                cpText.text = CPHelper.GetCP((Equipment) item).ToString();
            }
            cpText.transform.parent.gameObject.SetActive(isEquipment);
        }

        public void TutorialActionClickCombinationResultPopupSubmitButton() =>
            Close();
    }
}
