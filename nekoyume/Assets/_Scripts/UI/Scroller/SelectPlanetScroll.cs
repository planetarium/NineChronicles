using System;
using System.Globalization;
using System.Linq;
using Nekoyume.Multiplanetary;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class SelectPlanetScroll :
        FancyScrollRect<SelectPlanetCell.ViewModel, SelectPlanetScroll.ContextModel>
    {
        public class ContextModel : FancyScrollRectContext, IDisposable
        {
            public readonly Subject<(
                SelectPlanetCell cell,
                SelectPlanetCell.ViewModel viewModel)> OnClickCellSubject = new();

            public void Dispose()
            {
                OnClickCellSubject.Dispose();
            }
        }

        [SerializeField]
        private float cellSize;

        [SerializeField]
        private GameObject cellPrefab;

        protected override float CellSize => cellSize;

        protected override GameObject CellPrefab => cellPrefab;

        public readonly Subject<SelectPlanetScroll> OnClickSelectedPlanetSubject = new();

        public readonly Subject<(SelectPlanetScroll scroll, PlanetId selectedPlanetId)>
            OnChangeSelectedPlanetSubject = new();

        private void OnDestroy()
        {
            Context.Dispose();
            OnClickSelectedPlanetSubject.Dispose();
            OnChangeSelectedPlanetSubject.Dispose();
        }

        protected override void Initialize()
        {
            base.Initialize();
            Context.OnClickCellSubject.Subscribe(OnClickCell)
                .AddTo(gameObject);
        }

        public void SetData(PlanetRegistry planetRegistry, PlanetId? selectedPlanetId)
        {
            if (!initialized)
            {
                Initialize();
                initialized = true;
            }

            if (planetRegistry is null)
            {
                UpdateContents(Array.Empty<SelectPlanetCell.ViewModel>());
                return;
            }

            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            var selectedIndex = 0;
            var newItemsSource = planetRegistry.PlanetInfos.Select((e, index) =>
            {
                if (e is null)
                {
                    return new SelectPlanetCell.ViewModel
                    {
                        PlanetId = default,
                        PlanetName = "null",
                        IsSelected = false,
                        IsNew = false,
                    };
                }

                if (selectedPlanetId is null)
                {
                    return new SelectPlanetCell.ViewModel
                    {
                        PlanetId = e.ID,
                        PlanetName = textInfo.ToTitleCase(e.Name),
                        IsSelected = false,
                        IsNew = !(e.ID.Equals(PlanetId.Odin) ||
                                  e.ID.Equals(PlanetId.OdinInternal)),
                    };
                }

                var vm = new SelectPlanetCell.ViewModel
                {
                    PlanetId = e.ID,
                    PlanetName = textInfo.ToTitleCase(e.Name),
                    IsSelected = e.ID.Equals(selectedPlanetId),
                    IsNew = !(e.ID.Equals(PlanetId.Odin) ||
                              e.ID.Equals(PlanetId.OdinInternal)),
                };
                if (vm.IsSelected)
                {
                    selectedIndex = index;
                }

                return vm;
            }).ToArray();
            UpdateContents(newItemsSource);
            JumpTo(selectedIndex);
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

            var selectedPlanetId = viewModel.PlanetId;
            var newItemsSource = ItemsSource.Select(e =>
            {
                e.IsSelected = e.PlanetId.Equals(selectedPlanetId);
                return e;
            }).ToArray();
            UpdateContents(newItemsSource);
            OnChangeSelectedPlanetSubject.OnNext((this, tuple.viewModel.PlanetId));
        }
    }
}
