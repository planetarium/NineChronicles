using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    using UniRx;
    public class SimpleItemCell : GridCell<Model.Item, SimpleItemScroll.ContextModel>
    {
        [SerializeField]
        private SimpleItemView view;

        public override void UpdateContent(Model.Item viewModel)
        {
            view.SetData(viewModel);
            view.OnClick.Select(_ => viewModel)
                .Subscribe(Context.OnClick.OnNext)
                .AddTo(view.DisposablesAtSetData);
        }
    }
}
