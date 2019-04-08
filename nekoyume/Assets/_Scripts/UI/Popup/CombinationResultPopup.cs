using System;
using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.UI.ItemView;
using Nekoyume.UI.Model;
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
        
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private CombinationResultPopup<Model.Inventory.Item> _data;

        #region Mono

        private void Awake()
        {   
            this.ComponentFieldsNotNullTest();
        }

        private void OnDestroy()
        {
            _disposables.ForEach(d => d.Dispose());
        }

        #endregion

        public void Pop(CombinationResultPopup<Model.Inventory.Item> data)
        {
            if (ReferenceEquals(data, null))
            {
                return;
            }

            SetData(data);
            base.Show();
        }

        private void SetData(CombinationResultPopup<Model.Inventory.Item> data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }
            
            _disposables.ForEach(d => d.Dispose());

            _data = data;

            okButton.OnClickAsObservable()
                .Subscribe(_ => { _data.OnClickSubmit.OnNext(_data); })
                .AddTo(_disposables);
            
            UpdateView();
        }

        private void UpdateView()
        {
            if (_data.IsSuccess)
            {
                var item = _data.ResultItem.Item;
                
                titleText.text = "조합 성공";
                resultItemView.SetData(_data.ResultItem);
                resultItemNameText.text = item.Data.Name;
                SetElemental(item.Data.elemental, 5);
                resultItemDescriptionText.text = item.ToItemInfo();
                resultItem.SetActive(true);
                materialText.text = "조합에 사용된 아이템";
            }
            else
            {
                titleText.text = "조합 실패";
                resultItem.SetActive(false);
                materialText.text = "소모된 아이템";
            }

            using (var e = _data.MaterialItems.GetEnumerator())
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
                        item.SetData(data.Item.Value, data.Count.Value);
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
                    image.sprite = sprite;
                    image.SetNativeSize();
                    image.gameObject.SetActive(true);
                    
                    continue;
                }
                
                image.gameObject.SetActive(false);
            }
        }

        private void Clear()
        {
            _disposables.ForEach(d => d.Dispose());
            
            _data = null;
            
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
        }
    }
}
