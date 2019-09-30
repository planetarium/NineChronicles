using Nekoyume.Game;
using Nekoyume.Helper;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nekoyume.Model;

namespace Nekoyume.UI
{
    public class BuffIcon : MonoBehaviour
    {
        public Image image;
        public TextMeshProUGUI remainedDurationText;
        public Buff Data { get; set; }
        public CharacterBase character;

        public void Show(Buff buff)
        {
            Data = buff;
            image.enabled = true;
            remainedDurationText.enabled = true;
            var sprite = SpriteHelper.GetBuffIcon(Data.Data.Id);
            image.overrideSprite = sprite;
        }

        public void UpdateStatus()
        {
            remainedDurationText.text = Data.remainedDuration.ToString();
        }
        
        public void Hide()
        {
            image.enabled = false;
            remainedDurationText.enabled = false;
            image.overrideSprite = null;
        }
    }
}
