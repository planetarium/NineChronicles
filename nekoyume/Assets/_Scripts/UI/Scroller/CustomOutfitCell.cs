using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    using UniRx;
    public class CustomOutfitCell : GridCell<Model.CustomOutfit, CustomOutfitScroll.ContextModel>
    {
        [SerializeField]
        private CustomOutfitView view;

        public override void UpdateContent(Model.CustomOutfit viewModel)
        {
            view.SetData(viewModel);
            view.OnClick.Select(_ => viewModel)
                 .Subscribe(Context.OnClick.OnNext)
                 .AddTo(view.DisposablesAtSetData);
        }
    }
}
