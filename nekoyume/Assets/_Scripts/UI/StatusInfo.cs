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
            var keyString = statKey.ToUpper();
            if (keyString == "LUCK")
            {
                key.text = "CRI";
                value.text = ToPercentage((float) statValue);
                additional.text = equipValue > 0f
                    ? $"(+{ToPercentage(equipValue)})"
                    : "";
            }
            else
            {
                key.text = keyString;
                value.text = ((int) statValue).ToString();
                additional.text = equipValue > 0f
                    ? $"(+{equipValue})"
                    : "";
            }
            gameObject.SetActive(true);
        }

        private string ToPercentage(float rawValue)
        {
            return (rawValue * 100f).ToString("0.#\\%");
        }
    }
}
