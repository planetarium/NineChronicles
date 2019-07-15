using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Data.Table;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationResultPopup : PopupWidget
    {
        public Text titleText;
        public GameObject resultItem;
        public SimpleCountableItemView resultItemView;
        public Text resultItemNameText;
        public Image[] resultItemElementalImages;
        public Text resultItemDescriptionText;
        public Text materialText;
        public SimpleCountableItemView[] materialItems;
        public Button submitButton;
        public Text submitButtonText;
        public GameObject resultItemVfx;
        
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private Model.CombinationResultPopup _data;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();

            submitButtonText.text = LocalizationManager.Localize("UI_OK");
            
            submitButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.onClickSubmit.OnNext(_data);
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForAwake);
        }

        private void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
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
            
            _data = data;
            
            UpdateView();
        }
        
        private void Clear()
        {
            _data = null;
            
            UpdateView();
        }

        private void UpdateView()
        {
            if (ReferenceEquals(_data, null))
            {
                titleText.text = LocalizationManager.Localize("UI_COMBINATION_ERROR");
                resultItemView.Clear();
                resultItemView.gameObject.SetActive(true);
                resultItemNameText.gameObject.SetActive(false);
                foreach (var image in resultItemElementalImages)
                {
                    image.gameObject.SetActive(false);
                }
                resultItemDescriptionText.text = "";
                foreach (var item in materialItems)
                {
                    item.Clear();
                }

                return;
            }
            
            resultItemVfx.SetActive(false);
            if (_data.isSuccess)
            {
                var item = (ItemUsable) _data.item.Value;
                
                titleText.text = LocalizationManager.Localize("UI_COMBINATION_SUCCESS");
                resultItemView.SetData(_data);
                resultItemNameText.text = item.Data.name;
                SetElemental(item.Data.elemental, item.Data.grade);
                resultItemDescriptionText.text = item.ToItemInfo();
                resultItem.SetActive(true);
                materialText.text = LocalizationManager.Localize("UI_COMBINATION_MATERIALS");
                resultItemVfx.SetActive(true);

                AudioController.instance.PlaySfx(AudioController.SfxCode.Success);
            }
            else
            {
                titleText.text = LocalizationManager.Localize("UI_COMBINATION_FAIL");
                resultItem.SetActive(false);
                materialText.text = LocalizationManager.Localize("UI_COMBINATION_MATERIALS");
                
                AudioController.instance.PlaySfx(AudioController.SfxCode.Failed);
            }

            using (var e = _data.materialItems.GetEnumerator())
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

        private void SetElemental(Elemental.ElementalType type, int count)
        {
            var sprite = Elemental.GetSprite(type);
            if (ReferenceEquals(sprite, null))
            {
                foreach (var image in resultItemElementalImages)
                {
                    image.gameObject.SetActive(false);
                }
                
                return;
            }
            
            for (var i = 0; i < resultItemElementalImages.Length; i++)
            {
                var image = resultItemElementalImages[i];
                if (i < count)
                {
                    image.overrideSprite = sprite;
                    image.SetNativeSize();
                    image.gameObject.SetActive(true);
                    
                    continue;
                }
                
                image.gameObject.SetActive(false);
            }
        }
    }
}
