using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.ItemView
{
    public class ItemView<T> : MonoBehaviour where T : Game.Item.Inventory.InventoryItem
    {
        protected static readonly Color DefaultColor = Color.white;
        protected static readonly Color DimColor = new Color(1f, 1f, 1f, 0.3f);

        public Image iconImage;

        protected T Data;

        protected virtual void Awake()
        {
            if (ReferenceEquals(iconImage, null))
            {
                throw new SerializeFieldNullException();
            }
        }

        public virtual void SetData(T data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }

            Data = data;
            
            var sprite = ItemBase.GetSprite(Data.Item);
            if (ReferenceEquals(sprite, null))
            {
                throw new FailedToLoadResourceException<Sprite>();
            }

            iconImage.sprite = sprite;
            iconImage.SetNativeSize();
            iconImage.enabled = true;
        }

        public virtual void SetDim(bool isDim)
        {
            iconImage.color = isDim ? DimColor : DefaultColor;
        }

        public virtual void Clear()
        {
            Data = null;
            
            iconImage.enabled = false;
        }
        
        public bool HasData()
        {
            return !ReferenceEquals(Data, null);
        }
    }
}
