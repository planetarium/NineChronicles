using Nekoyume.Game.VFX;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
        private int _lastFloor;

        public TextMeshProUGUI FloorText => floorText;

        private static readonly int AnimatorHashShow = Animator.StringToHash("Show");

        public void SetData(int currentFloor, int maxFloor, int lastFloor)
        {
            _lastFloor = lastFloor;
            _maxFloor = maxFloor;
            floorText.text = $"{currentFloor}";
            textAnimator.SetTrigger(AnimatorHashShow);
            for (var i = 0; i < floors.Length; i++)
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

            floorsCompleted[currentFloor - 1].enabled = true;
            floorsCompleted[currentFloor - 1].DOFade(0, 0.5f).SetLoops(-1, LoopType.Yoyo);
        }

        public void SetLastFloorCompleted()
        {
            floorsCompleted[_lastFloor - 1].DOKill();
            floorsCompleted[_lastFloor - 1].DOFade(1, 0.2f);
            floorsCompleted[_lastFloor - 1].enabled = true;
            vfxs[_lastFloor - 1].Play();
        }

        public void SetCompleted(int currentFloor)
        {
            if (currentFloor - 1 <= floorsCompleted.Length)
            {
                //현재 층 완료 처리
                floorsCompleted[currentFloor - 1].DOKill();
                floorsCompleted[currentFloor - 1].DOFade(1, 0.2f);
                floorsCompleted[currentFloor - 1].enabled = true;
                vfxs[currentFloor - 1].Play();

                //다음 층정보 표기
                floorText.text = $"{Mathf.Min(currentFloor + 1, _maxFloor)}";
                textAnimator.SetTrigger("Show");
                if (currentFloor < _maxFloor)
                {
                    floorsCompleted[currentFloor].enabled = true;
                    floorsCompleted[currentFloor].DOFade(0, 0.5f).SetLoops(-1, LoopType.Yoyo);
                }
            }
            else
            {
                NcDebug.LogError("currentFloor is out of range");
            }
        }
    }
}
