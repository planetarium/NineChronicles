using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(Graphic))]
    public class ColorSwitchableGraphic : MonoBehaviour, ISwitchable
    {
        [SerializeField]
        private Color switchedOnColor = default;

        [SerializeField]
        private Color switchedOffColor = default;

        private Graphic _graphicCache;

        public Graphic Graphic => _graphicCache == null
            ? _graphicCache = GetComponent<Graphic>()
            : _graphicCache;

        public bool IsSwitchedOn => Graphic.color.Equals(switchedOnColor);

        private void Reset()
        {
            switchedOnColor = switchedOffColor = Graphic.color;
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
            Graphic.color = switchedOnColor;
        }

        public void SetSwitchOff()
        {
            Graphic.color = switchedOffColor;
        }
    }
}
