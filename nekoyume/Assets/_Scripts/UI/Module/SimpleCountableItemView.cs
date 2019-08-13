using Nekoyume.UI.Model;
using System;
using UniRx;

namespace Nekoyume.UI.Module
{
    public class SimpleCountableItemView : CountableItemView<Model.CountableItem>
    {
        protected IDisposable _disposableForDimmed;

        public override void SetData(CountableItem model)
        {
            base.SetData(model);

            _disposableForDimmed = model.dimmed.Subscribe(SetDim);
        }

        public override void Clear()
        {
            if (_disposableForDimmed != null)
            {
                _disposableForDimmed.Dispose();
                _disposableForDimmed = null;
            }
            base.Clear();
        }
    }
}
