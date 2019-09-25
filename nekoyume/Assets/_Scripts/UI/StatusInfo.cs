using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StatusInfo : MonoBehaviour
    {
        public Text key;
        public Text value;
        public Text additional;

        public void Set((string key, object value, float additional) tuple)
        {
            Set(tuple.key, tuple.value, tuple.additional);
        }

        public void Set(string statKey, object statValue, float equipValue)
        {
            var keyString = statKey.ToUpper();
            if (keyString == "LUCK")
            {
                key.text = "CRI";
                value.text = ToPercentage(decimal.ToSingle((decimal) statValue));
                additional.text = Mathf.Approximately(equipValue, 0f)
                    ? ""
                    : $"(+{ToPercentage(equipValue)})";
            }
            else
            {
                key.text = keyString;
                value.text = ((int) statValue).ToString();
                additional.text = Mathf.Approximately(equipValue, 0f)
                    ? ""
                    : $"(+{equipValue})";
            }
            gameObject.SetActive(true);
        }

        private string ToPercentage(float rawValue)
        {
            return (rawValue * 100f).ToString("0.#\\%");
        }
    }
}
