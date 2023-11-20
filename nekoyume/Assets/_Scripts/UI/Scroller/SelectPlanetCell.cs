using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class SelectPlanetCell : RectCell<SelectPlanetCell.ViewModel, SelectPlanetScroll.ContextModel>
    {
        public class ViewModel
        {
            public bool IsSelected;
            public string PlanetName;
            public bool IsNew;
        }

        [SerializeField]
        private ConditionalButton button;

        [SerializeField]
        private GameObject newMarkGO;

        private ViewModel _viewModel;

        public override void Initialize()
        {
            base.Initialize();
            button.OnClickSubject
                .Subscribe(_ => Context.OnClickCellSubject.OnNext((this, _viewModel)))
                .AddTo(gameObject);
            button.OnClickDisabledSubject
                .Subscribe(_ => Context.OnClickCellSubject.OnNext((this, _viewModel)))
                .AddTo(gameObject);
        }

        public override void UpdateContent(ViewModel itemData)
        {
            button.Interactable = itemData.IsSelected;
            button.Text = string.IsNullOrEmpty(itemData.PlanetName)
                ? "null"
                : itemData.PlanetName;
            newMarkGO.SetActive(itemData.IsNew);
        }
    }
}
