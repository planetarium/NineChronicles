using Nekoyume.Model.Item;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class SweepItem : MonoBehaviour
    {
        [SerializeField]
        private SweepItemView view;

        public void Set(ItemBase itemBase, int count)
        {
            view.Set(itemBase, count);
        }
    }
}
