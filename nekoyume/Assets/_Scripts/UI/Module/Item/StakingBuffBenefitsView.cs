using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class StakingBuffBenefitsView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private TextMeshProUGUI benefitsText;
        private const string ContentFormat = "{0} <color=#1FFF00>+{1}</color>";

        public void Set(string description, int count, int benefitsRate)
        {
            contentText.text = string.Format(ContentFormat, description, count);
            benefitsText.text = $"{benefitsRate}%";
        }
    }
}
