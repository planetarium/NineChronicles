using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nekoyume;
using NineChronicles.ExternalServices.IAPService.Runtime;
using NineChronicles.ExternalServices.IAPService.Runtime.Models;
using UnityEditor;
using UnityEngine;

namespace NineChronicles.ExternalServices.IAPService.Editor
{
    public class IAPServiceToolWindow : EditorWindow
    {
        private string _host =
            "https://hihq3f4id5.execute-api.ap-northeast-2.amazonaws.com/development";

        private Store _store = Store.Test;

        private IAPServiceManager _iapService;

        private string _productsText;

        // private string _purchaseRequestResultText;
        private string _purchaseStatusResultsText;

        [MenuItem("Tools/9C/IAP Service Tool")]
        private static void Init()
        {
            GetWindow<IAPServiceToolWindow>("IAP Service Tool", true).Show();
        }

        private async void OnEnable()
        {
            _iapService = new IAPServiceManager(_host, _store);
            await _iapService.InitializeAsync();
        }

        private void OnGUI()
        {
            if (!_iapService.IsInitialized)
            {
                GUILayout.Label("IAP Service being initialized...");
                return;
            }

            if (GUILayout.Button("Fetch products"))
            {
                Task.Run(GetProductsAsync);
            }

            EditorGUILayout.TextArea(_productsText);

            if (GUILayout.Button("PurchaseStatus"))
            {
                Task.Run(PurchaseStatusAsync);
            }

            EditorGUILayout.TextArea(_purchaseStatusResultsText);
        }

        private async Task GetProductsAsync()
        {
            var productResponse = await _iapService.GetProductsAsync(
                Addresses.Admin,
                force: true);
            _productsText = productResponse?.ToString() ?? "null";
        }

        private async Task PurchaseStatusAsync()
        {
            var uuids = new HashSet<string>();
            uuids.Add("test_uuid_1");
            uuids.Add("test_uuid_2");
            var receiptDetailSchemas = await _iapService.PurchaseStatusAsync(uuids);
            _purchaseStatusResultsText = receiptDetailSchemas is null
                ? "null"
                : string.Join("\n", receiptDetailSchemas.Select(e => e.ToString()));
        }
    }
}
