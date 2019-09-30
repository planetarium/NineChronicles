using Nekoyume.Game;
using Nekoyume.Helper;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BuffIcon : MonoBehaviour
    {
        public Image image;
        public Buff Data { get; set; }

        public void Show(Buff buff)
        {
            Data = buff;
            image.enabled = true;
            var sprite = SpriteHelper.GetBuffIcon(Data.Data.Id);
            image.overrideSprite = sprite;
        }
        
        public void Hide()
        {
            image.enabled = false;
            image.overrideSprite = null;
        }
    }
}