using System.Collections;
using Nekoyume.Action;
using UnityEngine;

namespace Nekoyume.Game.Entrance
{
    public class RoomEntering : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(Act());
        }

        private IEnumerator Act()
        {
            var stage = GetComponent<Stage>();
            var loadingScreen = UI.Widget.Find<UI.LoadingScreen>();
            loadingScreen.Show();

            UI.Widget.Find<UI.Menu>().ShowRoom();

            stage.id = 0;
            stage.LoadBackground("room");

            var objectPool = GetComponent<Util.ObjectPool>();
            var players = stage.GetComponentsInChildren<Character.Player>();
            foreach (var p in players)
            {
                objectPool.Remove<Character.Player>(p.gameObject);
            }
            objectPool.ReleaseAll();

            var boss = stage.GetComponentInChildren<Character.Boss.BossBase>();
            if (boss != null)
            {
                Destroy(boss.gameObject);
            }

            var playerFactory = GetComponent<Factory.PlayerFactory>();
            GameObject player = playerFactory.Create(ActionManager.instance.Avatar);
            player.transform.position = stage.RoomPosition;

            var status = UI.Widget.Find<UI.Status>();
            status.UpdatePlayer(player);
            status.SetStage(ActionManager.instance.Avatar.WorldStage);

            ActionCamera.instance.SetPoint(0f, 0f);
            ActionCamera.instance.Idle();

            yield return new WaitForSeconds(2.0f);
            loadingScreen.Close();
            Destroy(this);
        }
    }
}
