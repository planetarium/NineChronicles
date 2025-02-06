using Nekoyume.Multiplanetary;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public class SelectPlanetCell : FancyScrollRectCell<SelectPlanetCell.ViewModel, SelectPlanetScroll.ContextModel>
    {
        public class ViewModel
        {
            public PlanetId PlanetId;
            public string PlanetName;
            public bool IsNew;
            public bool IsSelected;
            public bool HasError;
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
                .Subscribe(_ => Context.OnClickDisableCellSubject.OnNext((this, _viewModel)))
                .AddTo(gameObject);
        }

        public override void UpdateContent(ViewModel itemData)
        {
            _viewModel = itemData;
            if (_viewModel is null)
            {
                NcDebug.Log("[SelectPlanetCell] UpdateContent()... _viewModel is null.");
                button.Interactable = false;
                button.Text = "null";
                newMarkGO.SetActive(false);
                return;
            }

            button.Text = string.IsNullOrEmpty(_viewModel.PlanetName)
                ? "null"
                : _viewModel.PlanetName;

            if (_viewModel.HasError)
            {
                button.Interactable = false;
                button.Text = $"{button.Text} (Error)";
                newMarkGO.SetActive(false);
                return;
            }

            button.Interactable = _viewModel.IsSelected;
            newMarkGO.SetActive(_viewModel.IsNew);
        }
    }
}
