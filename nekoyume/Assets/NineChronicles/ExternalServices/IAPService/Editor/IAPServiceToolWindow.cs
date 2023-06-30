using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
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

        private IAPServiceManager _iapService;

        private IReadOnlyList<ProductSchema> _products;
        // private PurchaseResponse200 _purchaseResponse200;

        [MenuItem("Tools/9C/IAP Service Tool")]
        private static void Init()
        {
            GetWindow<IAPServiceToolWindow>("IAP Service Tool", true).Show();
        }

        private async void Awake()
        {
            _iapService = new IAPServiceManager(_host);
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

            EditorGUILayout.TextArea(_products?.Count.ToString() ?? "null");

            // if (GUILayout.Button("Purchase"))
            // {
            //     Task.Run(PurchaseAsync);
            // }
            //
            // EditorGUILayout.TextArea(_purchaseResponse200?.ToString() ?? "null");
        }

        private async Task GetProductsAsync()
        {
            var productResponse = await _iapService.GetProductsAsync(
                Addresses.Admin,
                force: true);
            _products = productResponse;
        }

        // private async Task PurchaseAsync()
        // {
        //     var iapService = IAPServiceManager.Instance;
        //     var purchaseResponse = await iapService.PurchaseAsync();
        //     _purchaseResponse200 = purchaseResponse;
        // }
    }
}
