using System.Collections.Generic;
using System.Threading.Tasks;
using Nekoyume.Game;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class TableSheetsHelper
    {
        private const string AddressableAssetsContainerPath = nameof(AddressableAssetsContainer);

        public static async Task<TableSheets> MakeTableSheetsAsync(MonoBehaviour monoBehaviour)
        {
            var request = Resources.LoadAsync<AddressableAssetsContainer>(AddressableAssetsContainerPath);
            await monoBehaviour.StartCoroutineAsync(request);
            if (!(request.asset is AddressableAssetsContainer addressableAssetsContainer))
                throw new FailedToLoadResourceException<AddressableAssetsContainer>(AddressableAssetsContainerPath);

            return InitializeInternal(addressableAssetsContainer.tableCsvAssets);
        }

        public static TableSheets MakeTableSheets()
        {
            var csv = ImportSheets();
            return TableSheets.MakeTableSheets(csv);
        }

        public static Dictionary<string, string> ImportSheets()
        {
            var container = Resources.Load<AddressableAssetsContainer>(AddressableAssetsContainerPath);
            if (container is null)
            {
                throw new FailedToLoadResourceException<AddressableAssetsContainer>(AddressableAssetsContainerPath);
            }

            var csv = new Dictionary<string, string>();
            foreach (var asset in container.tableCsvAssets)
            {
                csv[asset.name] = asset.text;
            }

            return csv;
        }

        private static TableSheets InitializeInternal(List<TextAsset> tableCsvAssets)
        {
            var csv = new Dictionary<string, string>();
            foreach (var asset in tableCsvAssets)
            {
                csv[asset.name] = asset.text;
            }

            var tableSheets = TableSheets.MakeTableSheets(csv);
            return tableSheets;
        }
    }
}
