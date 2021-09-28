using UnityEngine;

namespace Nekoyume.UI.Module
{
    [CreateAssetMenu(fileName = "ItemOptionIconViewSO", menuName = "ScriptableObjects/ItemOptionIconViewSO")]
    public class ItemOptionIconViewSO : ScriptableObject
    {
        public Color statForegroundColor;
        public Color skillForegroundColor;
    }
}
