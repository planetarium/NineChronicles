using TMPro;
using UnityEngine;

namespace Nekoyume.UI.ItemView
{
    public class CountableItemView<T> : ItemView<T> where T : Game.Item.Inventory.InventoryItem
    {
        private const string CountTextFormat = "x{0}";
        
        public TextMeshProUGUI countText = null;

        protected int Count
        {
            set
            {
                countText.text = string.Format(CountTextFormat, value);
                countText.enabled = true;
            }
        }

        #region Mono
        
        protected override void Awake()
        {
            base.Awake();
            
            if (ReferenceEquals(countText, null))
            {
                throw new SerializeFieldNullException();
            }
        }
        
        #endregion

        #region override

        public override void SetData(T data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }
            
            base.SetData(data);

            Count = data.Count;
        }
        
        public override void SetDim(bool isDim)
        {
            base.SetDim(isDim);
            
            countText.color = isDim ? DimColor : DefaultColor;
        }

        public override void Clear()
        {
            base.Clear();
            
            countText.enabled = false;
        }

        #endregion

        protected void SetData(T data, int count)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;
            }
            
            base.SetData(data);

            Count = count;
        }
    }
}
