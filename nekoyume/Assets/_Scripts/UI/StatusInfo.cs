using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StatusInfo : MonoBehaviour
    {
        public Text key;
        public Text value;
        public Text additional;

        public void Set(string statKey, object statValue, float equipValue)
        {
            key.text = statKey.ToUpper();
            if (statKey == "luck")
            {
                statValue = (float)statValue * 100;
            }
            value.text = statValue.ToString();
            additional.text = equipValue > 0.0f ? $"(+{equipValue})" : "";
            gameObject.SetActive(true);
        }
    }
}
