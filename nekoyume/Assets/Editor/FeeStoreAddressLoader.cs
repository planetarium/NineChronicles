using System.Text;
using Nekoyume;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class FeeStoreAddressLoader
    {

        [MenuItem("Tools/Get Fee Store Address")]
        public static void LoadAddress()
        {
            var sb = new StringBuilder();
            var shopZero = Addresses.GetShopFeeAddress(0, 0);
            sb.Append("<SHOP>\n");
            sb.Append($"Shop_0_0 : {shopZero}\n");
            for (var i = 1; i < 10; i++)
            {
                for (var j = 1; j < 8; j++)
                {
                    var address = Addresses.GetShopFeeAddress(i, j);
                    sb.Append($"Shop_{i}_{j} : {address}\n");
                }
            }

            Debug.Log(sb);

            sb.Length = 0;
            var blacksmithZero = Addresses.GetBlacksmithFeeAddress(0, 0);
            sb.Append("<BLACKSMITH>\n");
            sb.Append($"Blacksmith_0_0 : {blacksmithZero}");
            for (var i = 0; i < 10; i++)
            {
                for (var j = 0; j < 8; j++)
                {
                    var address = Addresses.GetBlacksmithFeeAddress(i, j);
                    sb.Append($"Blacksmith{i}_{j} : {address}");
                }
            }

            Debug.Log(sb);
        }
    }
}
