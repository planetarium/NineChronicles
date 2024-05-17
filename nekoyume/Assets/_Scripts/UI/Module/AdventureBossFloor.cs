using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class AdventureBossFloor : MonoBehaviour
    {
        public enum FloorState
        {
            Clear,
            NotClear,
            Lock
        }

        [SerializeField] private GameObject floorOpen;
        [SerializeField] private GameObject floorClear;
        [SerializeField] private GameObject floorNotClear;
        [SerializeField] private GameObject floorLock;
        [SerializeField] private GameObject openNameImage;
        [SerializeField] private TextMeshProUGUI openNameText;


        public void SetState(FloorState floorState)
        {
            switch (floorState)
            {
                case FloorState.Clear:
                    floorOpen.SetActive(true);
                    floorClear.SetActive(true);
                    floorNotClear.SetActive(false);
                    floorLock.SetActive(false);
                    break;
                case FloorState.NotClear:
                    floorOpen.SetActive(true);
                    floorClear.SetActive(false);
                    floorNotClear.SetActive(true);
                    floorLock.SetActive(false);
                    break;
                case FloorState.Lock:
                    floorOpen.SetActive(false);
                    floorClear.SetActive(false);
                    floorNotClear.SetActive(false);
                    floorLock.SetActive(true);
                    break;
            }
        }
    }
}
