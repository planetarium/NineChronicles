using System;
using System.Globalization;
using System.Linq;
using Nekoyume.Multiplanetary;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    using UniRx;

    public class PlanetAccountInfoScroll :
        FancyGridView<PlanetAccountInfoCell.ViewModel, PlanetAccountInfoScroll.ContextModel>
    {
        public class ContextModel : FancyGridViewContext, IDisposable
        {
            public readonly Subject<(
                PlanetAccountInfoCell cell,
                PlanetAccountInfoCell.ViewModel viewModel)> OnClickCreateAccountSubject = new();

            public readonly Subject<(
                PlanetAccountInfoCell cell,
                PlanetAccountInfoCell.ViewModel viewModel)> OnClickImportKeySubject = new();

            public readonly Subject<(
                PlanetAccountInfoCell cell,
                PlanetAccountInfoCell.ViewModel viewModel)> OnClickSelectPlanetSubject = new();

            public void Dispose()
            {
                OnClickCreateAccountSubject.Dispose();
                OnClickImportKeySubject.Dispose();
                OnClickSelectPlanetSubject.Dispose();
            }
        }

        protected class GridCellGroup : DefaultCellGroup
        {
        }

        [SerializeField]
        private PlanetAccountInfoCell cellPrefab;

        public readonly Subject<(PlanetAccountInfoScroll scroll, PlanetId selectedPlanetId)>
            OnSelectedPlanetSubject = new();

        protected override void Initialize()
        {
            base.Initialize();
            Context.OnClickCreateAccountSubject
                .Subscribe(tuple =>
                    OnSelectedPlanetSubject.OnNext((this, tuple.viewModel.PlanetAccountInfo.PlanetId)))
                .AddTo(gameObject);
            Context.OnClickImportKeySubject
                .Subscribe(tuple =>
                    OnSelectedPlanetSubject.OnNext((this, tuple.viewModel.PlanetAccountInfo.PlanetId)))
                .AddTo(gameObject);
            Context.OnClickSelectPlanetSubject
                .Subscribe(tuple =>
                    OnSelectedPlanetSubject.OnNext((this, tuple.viewModel.PlanetAccountInfo.PlanetId)))
                .AddTo(gameObject);
        }

        protected override void SetupCellTemplate()
        {
            Setup<GridCellGroup>(cellPrefab);
        }

        public void SetData(
            PlanetRegistry planetRegistry,
            PlanetAccountInfo[] planetAccountInfos,
            bool needToImportKey)
        {
            if (!initialized)
            {
                Initialize();
                initialized = true;
            }

            if (planetRegistry is null ||
                planetAccountInfos is null)
            {
                UpdateContents(Array.Empty<PlanetAccountInfoCell.ViewModel>());
                return;
            }

            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            var newItemsSource = planetAccountInfos.Select(e =>
            {
                if (!planetRegistry.TryGetPlanetInfoById(e.PlanetId, out var planetInfo))
                {
                    return new PlanetAccountInfoCell.ViewModel
                    {
                        PlanetName = "null",
                        PlanetAccountInfo = e,
                        NeedToImportKey = needToImportKey
                    };
                }

                return new PlanetAccountInfoCell.ViewModel
                {
                    PlanetName = textInfo.ToTitleCase(planetInfo.Name),
                    PlanetAccountInfo = e,
                    NeedToImportKey = needToImportKey
                };
            }).ToArray();
            UpdateContents(newItemsSource);
        }
    }
}
