using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class SwitchableImage : MonoBehaviour, ISwitchable
    {
        public Image switchedOnImage;
        public Image switchedOffImage;

        public bool IsSwitchedOn => switchedOnImage.enabled;
        
        public void Switch()
        {
            var isSwitchedOn = IsSwitchedOn;
            switchedOnImage.enabled = !isSwitchedOn;
            switchedOffImage.enabled = isSwitchedOn;
        }

        public void SetSwitchOn()
        {
            switchedOnImage.enabled = true;
            switchedOffImage.enabled = false;
        }

        public void SetSwitchOff()
        {
            switchedOnImage.enabled = false;
            switchedOffImage.enabled = true;
        }
    }
}
