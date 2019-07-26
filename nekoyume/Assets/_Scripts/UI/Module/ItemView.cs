using System;
using Nekoyume.Game.Item;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemView<T> : MonoBehaviour where T : Model.Item
    {
        protected static readonly Color DefaultColor = Color.white;
        protected static readonly Color DimColor = new Color(1f, 1f, 1f, 0.3f);

        public Image iconImage;

        public RectTransform RectTransform { get; private set; }
        public T Model { get; private set; }

        #region Mono
        
        protected virtual void Awake()
        {
            this.ComponentFieldsNotNullTest();
            
            RectTransform = GetComponent<RectTransform>();
        }

        protected virtual void OnDestroy()
        {
            Clear();
        }

        #endregion

        public virtual void SetData(T model)
        {
            if (ReferenceEquals(model, null))
            {
                Clear();
                return;
            }

            Model = model;

            UpdateView();
        }

        public virtual void Clear()
        {
            Model = null;

            UpdateView();
        }
        
        protected virtual void SetDim(bool isDim)
        {
            iconImage.color = isDim ? DimColor : DefaultColor;
        }

        private void UpdateView()
        {
            if (ReferenceEquals(Model, null))
            {
                iconImage.enabled = false;
                
                return;
            }
            
            var sprite = ItemBase.GetSprite(Model.item.Value);
            if (ReferenceEquals(sprite, null))
            {
                throw new FailedToLoadResourceException<Sprite>(Model.item.Value.Data.id.ToString());
            }

            iconImage.sprite = sprite;
            iconImage.SetNativeSize();
            iconImage.enabled = true;
        }
    }
}
