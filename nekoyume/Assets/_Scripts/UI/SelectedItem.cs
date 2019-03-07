using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class SelectedItem : MonoBehaviour
    {
        public Image icon;
        public Text itemName;
        public Text info;
        public Text price;
        public ItemBase item;
        public Text flavour;
    }
}
