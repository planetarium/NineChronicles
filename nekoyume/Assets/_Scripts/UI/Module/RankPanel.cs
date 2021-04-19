using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class RankPanel : MonoBehaviour
    {
        [SerializeField]
        private List<NCToggle> toggles = new List<NCToggle>();

        private void Awake()
        {
            foreach (var toggle in toggles)
            {
                if (toggle is NCToggleDropdown toggleDropdown)
                {
                    toggleDropdown.onValueChanged.AddListener(value =>
                    {
                        if (value)
                        {
                            var firstElement = toggleDropdown.items.First();
                            if (firstElement is null)
                            {
                                Debug.LogError($"No sub element exists in {toggleDropdown.name}.");
                                return;
                            }

                            firstElement.isOn = true;
                        }
                    });
                }
            }
        }

        public void Show()
        {
            var firstElement = toggles.First();
            if (firstElement is null)
            {
                Debug.LogError($"No element exists in {name}");
                return;
            }

            firstElement.isOn = true;
        }
    }
}
