using System;
using System.Collections.Generic;
using TMPro;
using UniRx;

namespace Nekoyume.UI.Module
{
    public class CountableItemView<T> : ItemView<T> where T : Model.CountableItem
    {
        protected const string CountTextFormat = "{0}";

        public bool ignoreOne;
        public TextMeshProUGUI countText;

        protected override ImageSizeType imageSizeType => ImageSizeType.Small;

        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        #region override

        public override void SetData(T model)
        {
            if (ReferenceEquals(model, null))
            {
                Clear();
                return;
            }

            base.SetData(model);
            _disposablesForSetData.DisposeAllAndClear();
            Model.Count.Subscribe(SetCount).AddTo(_disposablesForSetData);
            Model.CountEnabled.SubscribeTo(countText).AddTo(_disposablesForSetData);

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

            countText.color = isDim ? DimmedColor : OriginColor;
        }

        #endregion

        protected virtual void SetCount(int count)
        {
            if(ignoreOne && count == 1)
            {
                countText.text = string.Empty;
                return;
            }
            countText.text = string.Format(CountTextFormat, count);
        }

        private void UpdateView()
        {
            if (Model is null)
            {
                countText.enabled = false;
                return;
            }

            SetCount(Model.Count.Value);
        }
    }
}
