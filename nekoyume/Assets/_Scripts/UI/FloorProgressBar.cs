using Nekoyume.Game.VFX;
using Nekoyume.L10n;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class FloorProgressBar : MonoBehaviour
    {
        [SerializeField]
        public GameObject[] floors;
        [SerializeField]
        public Image[] floorsCompleted;
        [SerializeField]
        public VFX[] vfxs;
        [SerializeField]
        public TextMeshProUGUI floorText;
        [SerializeField]
        public Animator textAnimator;

        private int _maxFloor;

        public void SetData(int currentFloor, int maxFloor)
        {
            _maxFloor = maxFloor;
            floorText.text = $"{currentFloor}";
            textAnimator.SetTrigger("Show");
            for (int i = 0; i < floors.Length; i++)
            {
                if (i >= currentFloor - 1 && i < maxFloor)
                {
                    floors[i].SetActive(true);
                    floorsCompleted[i].gameObject.SetActive(true);
                    floorsCompleted[i].enabled = false;
                    vfxs[i].Stop();
                }
                else
                {
                    floors[i].SetActive(false);
                    floorsCompleted[i].gameObject.SetActive(false);
                }
            }
        }

        public void SetCompleted(int currentFloor)
        {
            if (currentFloor - 1 <= floorsCompleted.Length)
            {
                floorText.text = $"{Mathf.Min(currentFloor + 1, _maxFloor)}";
                textAnimator.SetTrigger("Show");
                floorsCompleted[currentFloor - 1].enabled = true;
                vfxs[currentFloor - 1].Play();
            }
            else
            {
                NcDebug.LogError("currentFloor is out of range");
            }
        }

    }
}
