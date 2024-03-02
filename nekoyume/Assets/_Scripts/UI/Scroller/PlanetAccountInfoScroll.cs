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
        public class ContextModel : FancyGridViewContext
        {
        }

        protected class GridCellGroup : DefaultCellGroup
        {
        }

        [SerializeField]
        private PlanetAccountInfoCell cellPrefab;

        protected override void SetupCellTemplate() => Setup<GridCellGroup>(cellPrefab);

        public void SetData(
            PlanetRegistry planetRegistry,
            PlanetAccountInfo[] planetAccountInfos)
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
                    };
                }

                return new PlanetAccountInfoCell.ViewModel
                {
                    PlanetName = textInfo.ToTitleCase(planetInfo.Name),
                    PlanetAccountInfo = e,
                };
            }).ToArray();
            UpdateContents(newItemsSource);
        }
    }
}
