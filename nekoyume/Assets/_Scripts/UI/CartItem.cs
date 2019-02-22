using Nekoyume.Game.Item;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CartItem : MonoBehaviour
    {
        public Image icon;
        public Text itemName;
        public Text info;
        public Text price;
        public ItemBase item;
    }
}
