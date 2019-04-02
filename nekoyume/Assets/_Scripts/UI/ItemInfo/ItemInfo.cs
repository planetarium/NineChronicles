using System;
using System.Collections.Generic;
using Nekoyume.UI.ItemView;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.ItemInfo
{
    public class ItemInfo : MonoBehaviour
    {
        [SerializeField] public Text nameText;
        [SerializeField] public Text infoText;
        [SerializeField] public Text descriptionText;
        [SerializeField] public SimpleCountableItemView itemView;

        protected Model.ItemInfo _data;
        
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        
        #region Mono

        protected virtual void Awake()
        {
            if (ReferenceEquals(nameText, null) ||
                ReferenceEquals(infoText, null) ||
                ReferenceEquals(descriptionText, null) ||
                ReferenceEquals(itemView, null))
            {
                throw new SerializeFieldNullException();
            }
        }
        
        #endregion

        public virtual void SetData(Model.ItemInfo data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;       
            }
            
            _disposables.ForEach(d => d.Dispose());
            
            _data = data;
            _data.Item.Subscribe(_ => UpdateView()).AddTo(_disposables);

            UpdateView();
        }

        public virtual void UpdateView()
        {
            if (ReferenceEquals(_data, null) ||
                ReferenceEquals(_data.Item.Value, null))
            {
                nameText.text = "아이템 자세히 보기";
                infoText.text = "상단의 아이템을 클릭하세요";
                descriptionText.text = "";
                itemView.Clear();
                
                return;
            }
            
            var item = _data.Item.Value;
            nameText.text = item.Item.Data.Name;
            infoText.text = item.Item.ToItemInfo();
            descriptionText.text = item.Item.Data.Flavour;
            itemView.SetData(item);
            
//            if (_considerPrice)
//            {
//                priceIcon.enabled = false;
//                price.text = "";
//            }
        }
        
        public virtual void Clear()
        {
            _disposables.ForEach(d => d.Dispose());

            _data = null;
            
            UpdateView();
        }
    }
}
