using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public string AssetBundleURL
        {
            get { return assetBundleURL; }
        }

        public IReadOnlyCollection<string> AssetBundleNames
        {
            get { return assetBundleNames; }
        }
    }
}
