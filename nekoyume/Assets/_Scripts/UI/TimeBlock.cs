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

        private TimeBlockSet _selectedSet;

        private void Awake()
        {
            if (timeBlockType.Equals(TimeBlockType.Gauge))
            {
                gauge.Block.gameObject.SetActive(true);
                gauge.Time.gameObject.SetActive(true);
                top.Block.gameObject.SetActive(false);
                top.Time.gameObject.SetActive(false);
                _selectedSet = gauge;
            }
            else
            {
                gauge.Block.gameObject.SetActive(false);
                gauge.Time.gameObject.SetActive(false);
                top.Block.gameObject.SetActive(true);
                top.Time.gameObject.SetActive(true);
                _selectedSet = top;
            }
        }

        public void SetTimeBlock(string time, string block)
        {
            _selectedSet.Time.text = time;
            _selectedSet.Block.text = block;
        }
    }
}
