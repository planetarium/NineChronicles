using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationResultPopup : Widget
    {
        public Text titleText;
        public GameObject resultItem;
        public SimpleCountableItemView resultItemView;
        public Text resultItemNameText;
        public Image[] resultItemElementalImages;
        public Text resultItemDescriptionText;
        public Text materialText;
        public SimpleCountableItemView[] materialItems;
        public Button okButton;
        public GameObject resultItemVfx;
        
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private Model.CombinationResultPopup _data;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();
            okButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.onClickSubmit.OnNext(_data);
                    AudioController.PlayClick();
                    Find<Combination>()?.ShowResultVFX(_data);
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
                titleText.text = "조합 에러";
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
                var item = new Equipment(_data.item.Value.Data);
                
                titleText.text = "제작 성공!";
                resultItemView.SetData(_data);
                resultItemNameText.text = item.Data.name;
                SetElemental(item.Data.elemental, 5);
                resultItemDescriptionText.text = item.ToItemInfo();
                resultItem.SetActive(true);
                materialText.text = "제작 재료";
                resultItemVfx.SetActive(true);

                AudioController.instance.PlaySfx(AudioController.SfxCode.Success);
            }
            else
            {
                titleText.text = "제작 실패";
                resultItem.SetActive(false);
                materialText.text = "파괴된 재료";
                
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
            Sprite sprite = null;
            switch (type)
            {
                case Elemental.ElementalType.Fire:
                    sprite = Resources.Load<Sprite>("");
                    break;
                case Elemental.ElementalType.Land:
                    sprite = Resources.Load<Sprite>("");
                    break;
                case Elemental.ElementalType.Wind:
                    sprite = Resources.Load<Sprite>("");
                    break;
                case Elemental.ElementalType.Water:
                    sprite = Resources.Load<Sprite>("");
                    break;
            }
            
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
