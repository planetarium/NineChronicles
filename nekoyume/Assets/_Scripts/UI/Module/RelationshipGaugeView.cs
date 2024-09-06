using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class RelationshipGaugeView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI currentRelationshipText;

        [SerializeField]
        private TextMeshProUGUI maxRelationshipText;

        [SerializeField]
        private Slider slider;

        public void Set(long current, long max)
        {
            currentRelationshipText.SetText(current.ToString());
            maxRelationshipText.SetText(max.ToString());
            slider.maxValue = max;
            slider.value = current;
        }
    }
}
