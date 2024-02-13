using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Nekoyume.AddressablesHelper
{
    public static class AddressablesLoader
    {
        public static T Load<T>(string path)
            where T : UnityEngine.Object
        {
            var extension = "";
            var type = typeof(T);
            if (type == typeof(GameObject))
            {
                extension = "prefab";
            }
            else if (type == typeof(Sprite))
            {
                extension = "png";
            }

            var resName = $"Assets/Resources_moved/{path}.{extension}";
            Debug.Log(resName);
            var handle = Addressables.LoadAssetAsync<T>(resName);
            return handle.WaitForCompletion();
        }
    }
}
