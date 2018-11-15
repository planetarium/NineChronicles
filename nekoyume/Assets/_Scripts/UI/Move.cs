using Nekoyume.Move;
using UnityEngine;


namespace Nekoyume.UI
{
    public class Move : Widget
    {
        public GameObject btnMove;
        public GameObject btnHome;

        public void Start()
        {
            btnHome.SetActive(false);
        }

        public void SleepClick()
        {
            MoveManager.Instance.Sleep();
        }
    }
}
