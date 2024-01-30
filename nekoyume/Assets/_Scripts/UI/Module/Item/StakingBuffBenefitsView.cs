using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class StakingBuffBenefitsView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI contentText;

        public void Set(string description)
        {
            contentText.text = description;
        }
    }
}
