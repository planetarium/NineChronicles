using System;
using TMPro;
using UnityEngine;

namespace Nekoyume
{
    public class TimeBlock : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI content;


        public void SetTimeBlock(string time, string block)
        {
            content.text = $"{time}({block})";
        }
    }
}
