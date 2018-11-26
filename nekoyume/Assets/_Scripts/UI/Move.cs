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
        }

        public void ShowWorld()
        {
            Show();
            btnSleep.GetComponent<Button>().enabled = true;
            btnMove.GetComponent<Button>().enabled = false;
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
    }
}
