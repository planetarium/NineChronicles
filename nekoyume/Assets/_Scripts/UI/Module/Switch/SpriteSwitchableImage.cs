using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class SpriteSwitchableImage : MonoBehaviour, ISwitchable
    {
        public Image image;
        public Sprite switchedOnSprite;
        public Sprite switchedOffSprite;

        public bool IsSwitchedOn => image.sprite.Equals(switchedOnSprite);

        private void Reset()
        {
            image = GetComponent<Image>();
            switchedOnSprite = image.sprite;
            switchedOffSprite = switchedOnSprite;
        }

        public void Switch()
        {
            image.sprite = IsSwitchedOn
                ? switchedOnSprite
                : switchedOffSprite;
        }

        public void SetSwitchOn()
        {
            image.sprite = switchedOnSprite;
        }

        public void SetSwitchOff()
        {
            image.sprite = switchedOffSprite;
        }
    }
}
