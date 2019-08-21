using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationResultPopup : PopupWidget
    {
        public Text titleText;
        public ItemInformation itemInformation;
        public Text materialText;
        public SimpleCountableItemView[] materialItems;
        public Button submitButton;
        public Text submitButtonText;
        public GameObject resultItemVfx;
        
        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        private Model.CombinationResultPopup Model { get; set; }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();

            materialText.text = LocalizationManager.Localize("UI_COMBINATION_MATERIALS");
            submitButtonText.text = LocalizationManager.Localize("UI_OK");
            
            submitButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Close();
                })
                .AddTo(gameObject);
        }

        private void OnDestroy()
        {
            Clear();
        }

        #endregion

        public void Pop(Model.CombinationResultPopup data)
        {
            if (ReferenceEquals(data, null))
            {
                return;
            }
            
            AudioController.PlayPopup();

            SetData(data);
            base.Show();
        }

        private void SetData(Model.CombinationResultPopup data)
        {
            if (ReferenceEquals(data, null))
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
            if (ReferenceEquals(Model, null))
            {
                titleText.text = LocalizationManager.Localize("UI_COMBINATION_ERROR");
                itemInformation.gameObject.SetActive(false);
                
                return;
            }
            
            resultItemVfx.SetActive(false);
            if (Model.isSuccess)
            {
                titleText.text = LocalizationManager.Localize("UI_COMBINATION_SUCCESS");
                itemInformation.gameObject.SetActive(true);
                resultItemVfx.SetActive(true);
                AudioController.instance.PlaySfx(AudioController.SfxCode.Success);
            }
            else
            {
                titleText.text = LocalizationManager.Localize("UI_COMBINATION_FAIL");
                itemInformation.gameObject.SetActive(false);
                AudioController.instance.PlaySfx(AudioController.SfxCode.Failed);
            }

            using (var e = Model.materialItems.GetEnumerator())
            {
                foreach (var item in materialItems)
                {
                    e.MoveNext();
                    if (ReferenceEquals(e.Current, null))
                    {
                        item.Clear();
                    }
                    else
                    {
                        var data = e.Current;
                        item.SetData(data);
                    }
                }
            }
        }
    }
}
