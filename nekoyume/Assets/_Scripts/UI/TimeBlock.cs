using TMPro;
using UnityEngine;

namespace Nekoyume
{
    public class TimeBlock : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI content;

        public void SetTimeBlock(string block, string time)
        {
            content.text = $"{block}({time})";
        }
    }
}
