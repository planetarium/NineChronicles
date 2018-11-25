using System.Collections;
using UnityEngine;

namespace Nekoyume.Game.Entrance
{
    public class WorldEntering : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(Act());
        }

        private IEnumerator Act()
        {
            var stage = GetComponent<Stage>();
            var game = this.GetRootComponent<Game>();
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

                var objectPool = GetComponent<Util.ObjectPool>();
                objectPool.ReleaseAll();

                var playerFactory = GetComponent<Factory.PlayerFactory>();
                GameObject player = playerFactory.Create(stage);

                // position
                player.transform.position = new Vector2(-1.0f, Random.Range(-0.7f, -1.3f));

                var cam = Camera.main.gameObject.GetComponent<ActionCamera>();
                cam.target = player.transform;

                var spawners = GetComponentsInChildren<Trigger.MonsterSpawner>();
                foreach (var spawner in spawners)
                    spawner.SetData(data.MonsterPower);

                var exit = GetComponentInChildren<Trigger.StageExit>();
                exit.SetEnable();

                yield return new WaitForSeconds(2.0f);
                yield return StartCoroutine(blind.FadeOut(1.0f));

                Event.OnStageStart.Invoke();
            }
            Destroy(this);
        }
    }
}
