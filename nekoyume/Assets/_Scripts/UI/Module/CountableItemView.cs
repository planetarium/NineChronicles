using System;
using System.Collections.Generic;
using TMPro;
using UniRx;

namespace Nekoyume.UI.Module
{
    public class CountableItemView<T> : ItemView<T> where T : Model.CountableItem
    {
        private const string CountTextFormat = "x{0}";
        
        public TextMeshProUGUI countText;
        
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        #region override

        public override void SetData(T value)
        {
            if (ReferenceEquals(value, null))
            {
                Clear();
                return;
            }
            
            base.SetData(value);
            data.count.Subscribe(SetCount).AddTo(_disposables);
            
            UpdateView();
        }

        public override void Clear()
        {
            _disposables.DisposeAllAndClear();
            base.Clear();
            
            UpdateView();
        }

        protected override void SetDim(bool isDim)
        {
            base.SetDim(isDim);
            
            countText.color = isDim ? DimColor : DefaultColor;
        }

        #endregion

        protected void SetCount(int count)
        {
            countText.text = string.Format(CountTextFormat, count);
            countText.enabled = true;
        }

        private void UpdateView()
        {
            if (ReferenceEquals(data, null))
            {
                countText.enabled = false;
                return;
            }
            
            SetCount(data.count.Value);
        }
    }
}
