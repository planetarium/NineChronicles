using Nekoyume.Game.VFX;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class FloorProgressBar : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] floors;
        [SerializeField]
        private Image[] floorsCompleted;
        [SerializeField]
        private VFX[] vfxs;
        [SerializeField]
        private TextMeshProUGUI floorText;
        [SerializeField]
        private Animator textAnimator;

        private int _maxFloor;

        public TextMeshProUGUI FloorText => floorText;

        private static readonly int AnimatorHashShow = Animator.StringToHash("Show");

        public void SetData(int currentFloor, int maxFloor)
        {
            _maxFloor = maxFloor;
            floorText.text = $"{currentFloor}";
            textAnimator.SetTrigger(AnimatorHashShow);
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
