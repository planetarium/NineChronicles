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
            var asset = Resources.Load<AddressableAssetsContainer>(AddressableAssetsContainerPath);
            if (asset is null)
                throw new FailedToLoadResourceException<AddressableAssetsContainer>(AddressableAssetsContainerPath);
            
            return InitializeInternal(asset.tableCsvAssets);
        }
        
        private static TableSheets InitializeInternal(List<TextAsset> tableCsvAssets)
        {
            var csv = new Dictionary<string, string>();
            foreach (var asset in tableCsvAssets)
            {
                csv[asset.name] = asset.text;
            }

            var tableSheets = new TableSheets(csv);
            return tableSheets;
        }

    }
}
