using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(Image))]
    public class SpriteSwitchableImage : MonoBehaviour, ISwitchable
    {
        [SerializeField]
        private bool setNativeSize = default;

        [SerializeField]
        private Sprite switchedOnSprite = null;

        [SerializeField]
        private Sprite switchedOffSprite = null;

        private Image _imageCache;

        public Image Image => _imageCache == null
            ? _imageCache = GetComponent<Image>()
            : _imageCache;

        public bool IsSwitchedOn => Image.overrideSprite.Equals(switchedOnSprite);

        private void Reset()
        {
            switchedOnSprite = switchedOffSprite = Image.sprite;
        }

        public void Switch()
        {
            if (IsSwitchedOn)
            {
                SetSwitchOff();
            }
            else
            {
                SetSwitchOn();
            }
        }

        public void SetSwitchOn()
        {
            Image.overrideSprite = switchedOnSprite;

            if (setNativeSize)
            {
                Image.SetNativeSize();
            }
        }

        public void SetSwitchOff()
        {
            Image.overrideSprite = switchedOffSprite;

            if (setNativeSize)
            {
                Image.SetNativeSize();
            }
        }
    }
}
