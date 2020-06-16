using System.Collections.Generic;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class GuidedQuestCell : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI nameText = null;

        [SerializeField]
        private List<VanillaItemView> rewards = null;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
