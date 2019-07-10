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

        public override void SetData(T model)
        {
            if (ReferenceEquals(model, null))
            {
                Clear();
                return;
            }

            _disposablesForSetData.DisposeAllAndClear();
            base.SetData(model);
            Model.count.Subscribe(SetCount).AddTo(_disposablesForSetData);
            Model.countEnabled.Subscribe(countEnabled => countText.enabled = countEnabled).AddTo(_disposablesForSetData);

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
        }

        private void UpdateView()
        {
            if (ReferenceEquals(Model, null))
            {
                countText.enabled = false;
                return;
            }

            SetCount(Model.count.Value);
        }
    }
}
