using System.Collections;
using Nekoyume.Move;
using UnityEngine;
using Avatar = Nekoyume.Model.Avatar;

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
            Avatar avatar = MoveManager.Instance.Avatar;
            int currentStage = avatar.world_stage;
            Data.Table.Stage data;
            var tables = this.GetRootComponent<Data.Tables>();
            if (!avatar.dead && tables.Stage.TryGetValue(currentStage, out data))
            {
                var blind = UI.Widget.Find<UI.Blind>();
                yield return StartCoroutine(blind.FadeIn(1.0f, $"STAGE {currentStage}"));

                UI.Widget.Find<UI.Move>().ShowWorld();

                stage.Id = currentStage;
                stage.LoadBackground(data.Background);

                var objectPool = GetComponent<Util.ObjectPool>();
                objectPool.ReleaseAll();

                var playerFactory = GetComponent<Factory.PlayerFactory>();
                GameObject player = playerFactory.Create(true);
                player.transform.position = new Vector2(0.0f, -0.7f);

                UI.Widget.Find<UI.Status>().UpdatePlayer(player);

                var cam = Camera.main.gameObject.GetComponent<ActionCamera>();
                cam.target = player.transform;

                var spawners = GetComponentsInChildren<Trigger.MonsterSpawner>();
                foreach (var spawner in spawners)
                    spawner.SetData(currentStage, data.MonsterPower);

                yield return new WaitForSeconds(2.0f);
                yield return StartCoroutine(blind.FadeOut(1.0f));

                Event.OnStageStart.Invoke();
            }
            Destroy(this);
        }
    }
}
