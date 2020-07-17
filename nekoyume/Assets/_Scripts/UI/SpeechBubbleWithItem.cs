using Nekoyume.UI;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;

namespace _Scripts.UI
{
    public class SpeechBubbleWithItem : SpeechBubble
    {
        [SerializeField]
        private SimpleItemView _itemView;

        public void SetItemMaterial(Item item)
        {
            _itemView.SetData(item);
        }
    }
}
