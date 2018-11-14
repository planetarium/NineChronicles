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

        public void HackAndSlashClick()
        {
            GameObject stageObj = GameObject.FindGameObjectWithTag("Stage");
            Game.Stage stage = stageObj.GetComponent<Game.Stage>();
            stage.Move();

            btnMove.gameObject.SetActive(false);
            new Network.Request.SessionMoves()
            {
                name = "hack_and_slash",
                ResponseCallback = OnHackAndSlash
            }.Send();
        }

        public void OnHackAndSlash(Network.Response.LastStatus response)
        {
            GameObject stageObj = GameObject.FindGameObjectWithTag("Stage");
            Game.Stage stage = stageObj.GetComponent<Game.Stage>();
            stage.OnHackAndSlash(response);
        }

        public void SleepClick()
        {
            MoveManager.Instance.Sleep();
        }
    }
}
