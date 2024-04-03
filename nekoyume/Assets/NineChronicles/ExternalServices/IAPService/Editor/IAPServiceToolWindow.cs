using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            WriteIndented = true,
        };

        private string _host =
            "https://6s8m89vay4.execute-api.ap-northeast-2.amazonaws.com/development";

        private Store _store = Store.Test;

        private IAPServiceManager _iapService;

        private string _productsText;

        // private string _purchaseRequestResultText;
        private string _purchaseStatusResultsText;

        private Vector2 _scrollPosition;

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

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
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

            EditorGUILayout.EndScrollView();
        }

        private async Task GetProductsAsync()
        {
            var categoryResponse = await _iapService.GetProductsAsync(Addresses.Admin, "0x100000000000");
            NcDebug.Log(Addresses.Admin);
            _productsText = categoryResponse is null
                ? "null"
                : JsonSerializer.Serialize(categoryResponse, JsonSerializerOptions);
        }

        private async Task PurchaseStatusAsync()
        {
            var uuids = new HashSet<string>();
            uuids.Add("3fa85f64-5717-4562-b3fc-2c963f66afa1");
            uuids.Add("3fa85f64-5717-4562-b3fc-2c963f66afa2");
            uuids.Add("3fa85f64-5717-4562-b3fc-2c963f66afa3");
            var receiptDetailSchemas = await _iapService.PurchaseStatusAsync(uuids);
            _purchaseStatusResultsText = receiptDetailSchemas is null
                ? "null"
                : JsonSerializer.Serialize(receiptDetailSchemas, JsonSerializerOptions);
        }
    }
}
