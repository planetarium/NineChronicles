using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Game.Trigger;
using Nekoyume.Model;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Entrance
{
    public class WorldEntering : MonoBehaviour
    {
        private List<BattleLog> battleLog;
        private void Start()
        {
            StartCoroutine(Act());
        }

        private IEnumerator Act()
        {
            var stage = GetComponent<Stage>();
            var isRoom = stage.BackgroundName == "room";

            var roomPlayer = stage.GetComponentInChildren<Character.Player>();
            if (roomPlayer != null)
            {
                roomPlayer.RunSpeed = 1.0f;
            }

            battleLog = ActionManager.Instance.battleLog;
            var startStage = battleLog.Find(l => l.type == BattleLog.LogType.StartStage);
            var currentStage = startStage.stage;
            Data.Table.Stage data;
            var tables = this.GetRootComponent<Tables>();
            if (tables.Stage.TryGetValue(currentStage, out data))
            {
                if (isRoom)
                {
                    var blind = Widget.Find<Blind>();
                    yield return StartCoroutine(blind.FadeIn(1.0f, $"STAGE {currentStage}"));
                }

                Widget.Find<Move>().ShowWorld();

                stage.Id = currentStage;
                if (isRoom)
                    stage.LoadBackground(data.Background);
                else
                    stage.LoadBackground(data.Background, 3.0f);

                var playerCharacter = GetComponentInChildren<Character.Player>();
                playerCharacter.RunSpeed = 1.2f;
                if (isRoom)
                {
                    playerCharacter.transform.position = new Vector2(0.0f, -0.7f);
                }
                var player = playerCharacter.gameObject;
                playerCharacter.Init(ActionManager.Instance.battleLog);

                Widget.Find<SkillController>().Show(player);
                Widget.Find<Status>().UpdatePlayer(player);

                var cam = Camera.main.gameObject.GetComponent<ActionCamera>();
                cam.target = player.transform;

                var spawner = GetComponentsInChildren<MonsterSpawner>().First();
                spawner.SetData(stage.Id, battleLog);

                yield return new WaitForSeconds(2.0f);
                if (isRoom)
                {
                    var blind = Widget.Find<Blind>();
                    yield return StartCoroutine(blind.FadeOut(1.0f));
                }

                Event.OnStageStart.Invoke();
            }

            Destroy(this);
        }
    }
}
