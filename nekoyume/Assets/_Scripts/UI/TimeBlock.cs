using System;
using TMPro;
using UnityEngine;

namespace Nekoyume
{
    public class TimeBlock : MonoBehaviour
    {
        private enum TimeBlockType
        {
            Gauge,
            Top,
        }

        [Serializable]
        public class TimeBlockSet
        {
            public TextMeshProUGUI Block;
            public TextMeshProUGUI Time;
        }

        [SerializeField]
        private TimeBlockType timeBlockType;


        [SerializeField]
        private TimeBlockSet gauge;

        [SerializeField]
        private TimeBlockSet top;

        private void Awake()
        {
            if (timeBlockType.Equals(TimeBlockType.Gauge))
            {
                gauge.Block.gameObject.SetActive(true);
                gauge.Time.gameObject.SetActive(true);
                top.Block.gameObject.SetActive(false);
                top.Time.gameObject.SetActive(false);
            }
            else
            {
                gauge.Block.gameObject.SetActive(false);
                gauge.Time.gameObject.SetActive(false);
                top.Block.gameObject.SetActive(true);
                top.Time.gameObject.SetActive(true);
            }
        }

        public void SetTimeBlock(string time, string block)
        {
            if (timeBlockType.Equals(TimeBlockType.Gauge))
            {
                gauge.Time.text = time;
                gauge.Block.text = block;
            }
            else
            {
                top.Time.text = time;
                top.Block.text = block;
            }
        }
    }
}
