using Nekoyume.UI;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Scripts.UI
{
    public class SpeechBubbleWithItem : SpeechBubble
    {
        [SerializeField]
        private SimpleItemView itemView = null;

        public void SetItemMaterial(Item item, bool isConsumable)
        {
            itemView.SetData(item, isConsumable);
        }

        public override void Hide()
        {
            base.Hide();

            itemView.Hide();
        }

        protected override void SetBubbleImageInternal()
        {
            base.SetBubbleImageInternal();

            itemView.Show();
        }
    }
}
