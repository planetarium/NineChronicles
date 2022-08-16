using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Nekoyume.Helper
{
    public static class AddressablesHelper
    {
        public static void LoadAssets(string label, List<GameObject> objects, System.Action callback)
        {
            var handle = Addressables.LoadAssetsAsync<GameObject>(label, objects.Add);
            handle.Completed += (result) =>
            {
                callback?.Invoke();
            };
        }


    }
}
