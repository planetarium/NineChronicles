using System.Collections;
using Nekoyume.Move;
using Nekoyume.UI;
using UnityEngine;
using Avatar = Nekoyume.Model.Avatar;

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
            var blind = Widget.Find<Blind>();
            yield return StartCoroutine(blind.FadeIn(1.0f, "ROOM"));

            Widget.Find<UI.Move>().ShowRoom();

            stage.Id = 0;
            stage.LoadBackground("room");

            var objectPool = GetComponent<Util.ObjectPool>();
            objectPool.ReleaseAll();

            var playerFactory = GetComponent<Factory.PlayerFactory>();
            GameObject player = playerFactory.Create(false);
            player.transform.position = new Vector2(0.0f, -0.7f);

            var cam = Camera.main.gameObject.GetComponent<ActionCamera>();
            var camPos = cam.transform.position;
            camPos.x = 0.0f;
            camPos.y = 0.0f;
            cam.transform.position = camPos;
            cam.target = null;

            yield return new WaitForSeconds(2.0f);
            yield return StartCoroutine(blind.FadeOut(1.0f));

            Event.OnStageStart.Invoke();
            Avatar avatar = MoveManager.Instance.Avatar;
            bool enabled = !avatar.dead;
            Widget.Find<UI.Move>().btnMove.SetActive(enabled);

            Destroy(this);
        }
    }
}
