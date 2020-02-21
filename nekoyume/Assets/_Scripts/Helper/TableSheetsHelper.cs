using System.Collections.Generic;
using System.Threading.Tasks;
using Nekoyume.TableData;
using UniRx.Async;
using UnityEngine;

namespace Nekoyume.Helper
{
    public static class TableSheetsHelper
    {
        private const string AddressableAssetsContainerPath = nameof(AddressableAssetsContainer);

        public static async Task<TableSheets> MakeTableSheetsAsync()
        {
            var request = Resources.LoadAsync<AddressableAssetsContainer>(AddressableAssetsContainerPath);
            await request;
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
            var tableSheets = new TableSheets();
            foreach (var asset in tableCsvAssets)
            {
                tableSheets.SetToSheet(asset.name, asset.text);
            }

            tableSheets.ItemSheetInitialize();
            tableSheets.QuestSheetInitialize();

            return tableSheets;
        }

    }
}
