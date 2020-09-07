using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(Selectable))]
    public class InteractableSwitchableSelectable : MonoBehaviour, ISwitchable
    {
        [SerializeField]
        private UnityEvent onSwitchedOn = null;

        [SerializeField]
        private UnityEvent onSwitchedOff = null;

        private Selectable _selectableCache;

        public Selectable Selectable => _selectableCache == null
            ? _selectableCache = GetComponent<Selectable>()
            : _selectableCache;

        public bool IsSwitchedOn => Selectable.interactable;

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
            Selectable.interactable = true;
            onSwitchedOn.Invoke();
        }

        public void SetSwitchOff()
        {
            Selectable.interactable = false;
            onSwitchedOff.Invoke();
        }
    }
}
