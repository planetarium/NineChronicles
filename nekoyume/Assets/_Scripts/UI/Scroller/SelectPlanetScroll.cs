using System;
using System.Linq;
using Nekoyume.Planet;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class SelectPlanetScroll : RectScroll<SelectPlanetCell.ViewModel, SelectPlanetScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext
        {
            public readonly Subject<(
                SelectPlanetCell cell,
                SelectPlanetCell.ViewModel viewModel)> OnClickCellSubject = new();

            public override void Dispose()
            {
                OnClickCellSubject.Dispose();
                base.Dispose();
            }
        }

        public readonly Subject<SelectPlanetScroll> OnClickSelectedPlanetSubject = new();

        public readonly Subject<(
            SelectPlanetScroll scroll,
            string selectedPlanetName)> OnChangeSelectedPlanetSubject = new();

        protected override void Initialize()
        {
            base.Initialize();
            Context.OnClickCellSubject.Subscribe(OnClickCell)
                .AddTo(gameObject);
        }

        public void UpdateData(PlanetRegistry planetRegistry)
        {
            if (planetRegistry is null)
            {
                UpdateData(Array.Empty<SelectPlanetCell.ViewModel>());
                return;
            }

            UpdateData(planetRegistry.PlanetInfos.Select(e =>
            {
                if (e is null)
                {
                    return new SelectPlanetCell.ViewModel
                    {
                        PlanetName = "null",
                        IsSelected = false,
                        IsNew = false,
                    };
                }

                if (string.IsNullOrEmpty(e.Name))
                {
                    return new SelectPlanetCell.ViewModel
                    {
                        PlanetName = "null",
                        IsSelected = false,
                        IsNew = false,
                    };
                }

                return new SelectPlanetCell.ViewModel
                {
                    PlanetName = e.Name,
                    IsSelected = false,
                    IsNew = !e.Name.Contains(nameof(PlanetId.Odin)),
                };
            }).ToArray());
        }

        private void OnClickCell((
            SelectPlanetCell cell,
            SelectPlanetCell.ViewModel viewModel) tuple)
        {
            var (_, viewModel) = tuple;
            if (viewModel.IsSelected)
            {
                OnClickSelectedPlanetSubject.OnNext(this);
                return;
            }

            viewModel.IsSelected = true;
            var newItemsSource = ItemsSource.Select(e =>
            {
                if (e.IsSelected)
                {
                    e.IsSelected = false;
                    return e;
                }

                return e;
            });

            UpdateData(newItemsSource);
            OnChangeSelectedPlanetSubject.OnNext((this, tuple.viewModel.PlanetName));
        }
    }
}
