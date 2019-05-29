using System.Collections;
using Nekoyume.Action;
using Nekoyume.UI;
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
            var stage = Game.instance.stage;
            var objectPool = Game.instance.stage.objectPool;
            
            Widget.Find<LoadingScreen>()?.Show();
            Widget.Find<Menu>()?.ShowRoom();

            stage.id = 0;
            stage.LoadBackground("room");

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

            var playerFactory = Game.instance.stage.playerFactory;
            GameObject player = playerFactory.Create(States.CurrentAvatarState.Value);
            player.transform.position = stage.roomPosition - new Vector2(3.0f, 0.0f);
            var playerComp = player.GetComponent<Character.Player>();
            playerComp.StartRun();

            var status = Widget.Find<Status>();
            status.UpdatePlayer(player);

            ActionCamera.instance.SetPoint(0f, 0f);
            ActionCamera.instance.Idle();

            yield return new WaitForSeconds(1.0f);
            Widget.Find<LoadingScreen>()?.Close();

            while (player.transform.position.x < stage.roomPosition.x)
            {
                yield return null;
            }
            playerComp.RunSpeed = 0.0f;
            playerComp.animator.Idle();

            Destroy(this);
        }
    }
}
