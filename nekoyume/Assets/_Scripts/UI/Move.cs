using Nekoyume.Game;
using Nekoyume.Move;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class Move : Widget
    {
        public GameObject btnMove;
        public GameObject btnSleep;

        public void ShowRoom()
        {
            Show();
            btnSleep.GetComponent<Button>().enabled = true;
            btnMove.GetComponent<Button>().enabled = true;
            Model.Avatar avatar = MoveManager.Instance.Avatar;
            bool enabled = !avatar.dead;
            btnMove.SetActive(enabled);
            btnSleep.SetActive(false);

        }

        public void ShowWorld()
        {
            Show();
            btnMove.SetActive(false);
            btnSleep.SetActive(true);
        }

        public void MoveClick()
        {
            Game.Event.OnStageEnter.Invoke();
            Close();
        }

        public void SleepClick()
        {
            Game.Event.OnPlayerSleep.Invoke();
            Close();
        }

        public void MoveStageClick()
        {
            MoveManager.Instance.MoveStage(1);
            Game.Event.OnStageEnter.Invoke();
        }
    }
}
