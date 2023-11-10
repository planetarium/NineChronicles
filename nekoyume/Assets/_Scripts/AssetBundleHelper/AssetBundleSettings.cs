using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.AssetBundleHelper
{
    [CreateAssetMenu(fileName = "AssetBundleSettings",
        menuName = "ScriptableObjects/AssetBundleSettings")]
    public class AssetBundleSettings : ScriptableObject
    {
        [SerializeField] private string assetBundleURL;
        [SerializeField] private string assetBundleCredentials;
        [SerializeField] private List<string> assetBundleNames;

        public string AssetBundleURL => assetBundleURL;
        public IReadOnlyCollection<string> AssetBundleNames => assetBundleNames;
    }
}
