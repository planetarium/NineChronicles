using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationResultPopup : PopupWidget
    {
        public VerticalLayoutGroup verticalLayoutGroup;
        public TextMeshProUGUI itemNameText;
        public CombinationItemInformation itemInformation;
        public TextMeshProUGUI materialText;
        public SimpleCountableItemView[] materialItems;
        public Button submitButton;
        public TextMeshProUGUI submitButtonText;
        public Image materialPlusImage;
        public GameObject materialView;
        public TouchHandler touchHandler;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        private Model.CombinationResultPopup Model { get; set; }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            materialText.text = LocalizationManager.Localize("UI_COMBINATION_MATERIALS");
            submitButtonText.text = LocalizationManager.Localize("UI_OK");

            submitButton.OnClickAsObservable().Subscribe(_ =>
            {
                Model.OnClickSubmit.OnNext(Model);
                AudioController.PlayClick();
                ItemMoveAnimation.Show(itemInformation.Model.item.Value.ItemBase.Value.GetIconSprite(),
                    itemInformation.transform.position, 
                    Find<BottomMenu>().inventoryButton.transform.position,
                    false,
                    1f, 
                    0.82f,
                    true);
                Close();
            }).AddTo(gameObject);
            touchHandler.OnClick.Subscribe(pointerEventData =>
            {
                if (!pointerEventData.pointerCurrentRaycast.gameObject.Equals(gameObject))
                    return;
                
                AudioController.PlayClick();
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

        public void Pop(Model.CombinationResultPopup data)
        {
            if (data is null)
            {
                return;
            }

            base.Show();
            SetData(data);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) verticalLayoutGroup.transform);
        }

        private void SetData(Model.CombinationResultPopup data)
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
                itemNameText.text = LocalizationManager.Localize("UI_COMBINATION_ERROR");
                itemInformation.gameObject.SetActive(false);
                return;
            }

            var item = Model.itemInformation.Value.item.Value.ItemBase.Value;
            var isEquipment = item is Equipment;
            materialPlusImage.gameObject.SetActive(isEquipment);

            if (Model.isSuccess)
            {
                itemNameText.text = item.GetLocalizedName();
                itemInformation.gameObject.SetActive(true);
                AudioController.instance.PlaySfx(AudioController.SfxCode.Success);
            }
            else
            {
                itemNameText.text = LocalizationManager.Localize("UI_COMBINATION_FAIL");
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
        }
    }
}
