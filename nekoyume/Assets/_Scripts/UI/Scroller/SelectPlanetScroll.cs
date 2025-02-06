using System;
using System.Globalization;
using System.Linq;
using Nekoyume.Game.LiveAsset;
using Nekoyume.L10n;
using Nekoyume.Multiplanetary;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class SelectPlanetScroll : FancyScrollRect<SelectPlanetCell.ViewModel, SelectPlanetScroll.ContextModel>
    {
        public class ContextModel : FancyScrollRectContext, IDisposable
        {
            public readonly Subject<(
                SelectPlanetCell cell,
                SelectPlanetCell.ViewModel viewModel)> OnClickCellSubject = new();
            public readonly Subject<(
                SelectPlanetCell cell,
                SelectPlanetCell.ViewModel viewModel)> OnClickDisableCellSubject = new();

            public void Dispose()
            {
                OnClickCellSubject.Dispose();
                OnClickDisableCellSubject.Dispose();
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
            Context.OnClickCellSubject.Subscribe(OnClickCell).AddTo(gameObject);
            Context.OnClickDisableCellSubject.Subscribe(OnClickDisableCell).AddTo(gameObject);
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
            var newItemsSource = planetRegistry.PlanetInfos.Where(e =>
            {
                if (LiveAssetManager.instance.ThorSchedule.IsOpened)
                {
                    return true;
                }

                return !IsThor(e);
            }).Select(e =>
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

                var isSelect = selectedPlanetId is not null && e.ID.Equals(selectedPlanetId);

                return new SelectPlanetCell.ViewModel
                {
                    PlanetId = e.ID,
                    PlanetName = textInfo.ToTitleCase(e.Name),
                    IsSelected = isSelect,
                    IsNew = IsThor(e),
                    HasError = e.ErrorType != null,
                };
            }).ToArray();
            UpdateContents(newItemsSource);

            bool IsThor(PlanetInfo e) => e.ID.Equals(PlanetId.Thor) ||
                e.ID.Equals(PlanetId.ThorInternal);
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
                if (e.IsSelected)
                {
                    e.IsSelected = false;
                    return e;
                }

                if (e.PlanetId.Equals(selectedPlanetId))
                {
                    e.IsSelected = true;
                    return e;
                }

                return e;
            }).ToArray();
            UpdateContents(newItemsSource);
            OnChangeSelectedPlanetSubject.OnNext((this, tuple.viewModel.PlanetId));
        }

        private void OnClickDisableCell((
            SelectPlanetCell cell,
            SelectPlanetCell.ViewModel viewModel) tuple)
        {
            Widget.Find<OneButtonSystem>().Show(L10nManager.Localize("EDESC_PLANET_ACCOUNT_INFOS_NOT_INITIALIZED"),
                L10nManager.Localize("UI_YES"),
                null);
        }
    }
}
