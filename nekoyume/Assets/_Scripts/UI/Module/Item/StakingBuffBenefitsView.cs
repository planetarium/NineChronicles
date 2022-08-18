using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class StakingBuffBenefitsView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private TextMeshProUGUI benefitsText;

        public void Set(string description, int benefitsRate)
        {
            contentText.text = description;
            benefitsText.text = $"{benefitsRate}%";
        }
    }
}
