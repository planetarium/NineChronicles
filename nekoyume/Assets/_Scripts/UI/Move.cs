using Nekoyume.Game;
using Nekoyume.Game.Trigger;
using Nekoyume.Move;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class Move : Widget
    {
        public GameObject btnMove;
        public GameObject btnStage1;
        public GameObject btnSleep;

        public Text LabelInfo;

        public void ShowRoom()
        {
            Show();
            btnSleep.GetComponent<Button>().enabled = true;
            btnMove.GetComponent<Button>().enabled = true;
            Model.Avatar avatar = MoveManager.Instance.Avatar;
            bool enabled = !avatar.Dead;
            btnMove.SetActive(enabled);
            btnStage1.SetActive(MoveManager.Instance.Avatar.WorldStage > 1);
            btnSleep.SetActive(false);
            LabelInfo.text = "";
        }

        public void ShowWorld()
        {
            Show();
            btnMove.SetActive(false);
            btnSleep.SetActive(true);
            LabelInfo.text = "";
        }

        public void MoveClick()
        {
            Game.Event.OnStageEnter.Invoke();
            Close();
        }

        public void SleepClick()
        {
            LabelInfo.text = "Go Home Soon...";
            this.GetRootComponent<Game.Game>().GetComponentInChildren<StageExit>().Sleep = true;
            btnSleep.SetActive(false);
        }

        public void MoveStageClick()
        {
            MoveManager.Instance.MoveZone(1);
            Game.Event.OnStageEnter.Invoke();
        }
    }
}
