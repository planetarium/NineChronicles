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
            bool isRoom = stage.BackgroundName == "room";

            var roomPlayer = stage.GetComponentInChildren<Character.Player>();
            if (roomPlayer != null)
            {
                roomPlayer.WalkSpeed = 1.0f;
            }

            Avatar avatar = MoveManager.Instance.Avatar;
            int currentStage = avatar.WorldStage;
            Data.Table.Stage data;
            var tables = this.GetRootComponent<Data.Tables>();
            if (!avatar.Dead && tables.Stage.TryGetValue(currentStage, out data))
            {
                if (isRoom)
                {
                    var blind = UI.Widget.Find<UI.Blind>();
                    yield return StartCoroutine(blind.FadeIn(1.0f, $"STAGE {currentStage}"));
                }

                UI.Widget.Find<UI.Move>().ShowWorld();

                stage.Id = currentStage;
                if (isRoom)
                {
                    stage.LoadBackground(data.Background);
                }
                else
                {
                    stage.LoadBackground(data.Background, 3.0f);
                }

                Character.Player playerCharacter = GetComponentInChildren<Character.Player>();
                playerCharacter.WalkSpeed = 1.2f;
                if (isRoom)
                {
                    playerCharacter.transform.position = new Vector2(0.0f, -0.7f);
                }
                GameObject player = playerCharacter.gameObject;

                UI.Widget.Find<UI.SkillController>().Show(player);
                UI.Widget.Find<UI.Status>().UpdatePlayer(player);

                var cam = Camera.main.gameObject.GetComponent<ActionCamera>();
                cam.target = player.transform;

                var spawners = GetComponentsInChildren<Trigger.MonsterSpawner>();
                foreach (var spawner in spawners)
                    spawner.SetData(currentStage, data.MonsterPower);

                yield return new WaitForSeconds(2.0f);
                if (isRoom)
                {
                    var blind = UI.Widget.Find<UI.Blind>();
                    yield return StartCoroutine(blind.FadeOut(1.0f));
                }

                Event.OnStageStart.Invoke();
            }
            Destroy(this);
        }
    }
}
