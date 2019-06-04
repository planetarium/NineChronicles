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
        
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region override

        public override void SetData(T value)
        {
            if (ReferenceEquals(value, null))
            {
                Clear();
                return;
            }
            
            _disposablesForSetData.DisposeAllAndClear();
            base.SetData(value);
            Data.count.Subscribe(SetCount).AddTo(_disposablesForSetData);
            
            UpdateView();
        }

        public override void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
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
            if (ReferenceEquals(Data, null))
            {
                countText.enabled = false;
                return;
            }
            
            SetCount(Data.count.Value);
        }
    }
}
