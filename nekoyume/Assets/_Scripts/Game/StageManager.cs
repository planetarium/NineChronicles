using System.Collections;
using UnityEngine;
using Avatar = Nekoyume.Model.Avatar;

namespace Nekoyume.Game
{
    public class StageManager : MonoBehaviour
    {
        public IEnumerator WorldEntering(Stage stage)
        {
            Game game = this.GetRootComponent<Game>();
            int currentStage = game.Avatar.world_stage;
            Data.Table.Stage data;
            var tables = this.GetRootComponent<Data.Tables>();
            if (tables.Stage.TryGetValue(currentStage, out data))
            {
                var blind = UI.Widget.Find<UI.Blind>();
                yield return StartCoroutine(blind.FadeIn(1.0f, $"STAGE {currentStage}"));

                UI.Widget.Find<UI.Move>().ShowWorld();

                stage.Id = currentStage;
                stage.LoadBackground(data.Background);

                var objectPool = GetComponent<ObjectPool>();
                objectPool.ReleaseAll();

                var playerFactory = GetComponent<Factory.PlayerFactory>();
                GameObject player = playerFactory.Create(stage);

                // position
                player.transform.position = new Vector2(-1.0f, Random.Range(-0.7f, -1.3f));

                var cam = Camera.main.gameObject.GetComponent<ActionCamera>();
                cam.target = player.transform;

                var spawners = GetComponentsInChildren<Trigger.MonsterSpawner>();
                foreach (var spawner in spawners)
                    spawner.SetData(currentStage, data.MonsterPower);

                var exit = GetComponentInChildren<Trigger.StageExit>();
                exit.SetEnable();

                yield return new WaitForSeconds(2.0f);
                yield return StartCoroutine(blind.FadeOut(1.0f));

                Event.OnStageStart.Invoke();
            }
        }

        public IEnumerator RoomEntering(Stage stage)
        {
            var game = this.GetRootComponent<Game>();
            var blind = UI.Widget.Find<UI.Blind>();
            yield return StartCoroutine(blind.FadeIn(1.0f, "ROOM"));

            UI.Widget.Find<UI.Move>().ShowRoom();

            stage.Id = 0;
            stage.LoadBackground("room");

            var objectPool = GetComponent<ObjectPool>();
            objectPool.ReleaseAll();

            var playerFactory = GetComponent<Factory.PlayerFactory>();
            GameObject player = playerFactory.Create(stage);

            // position
            player.transform.position = new Vector2(0.0f, -1.0f);

            yield return new WaitForSeconds(2.0f);
            yield return StartCoroutine(blind.FadeOut(1.0f));

            Event.OnStageStart.Invoke();
        }
    }
}
