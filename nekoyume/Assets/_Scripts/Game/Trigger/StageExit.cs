using System.Collections;
using Nekoyume.Game.Character;
using Nekoyume.Move;
using UnityEngine;


namespace Nekoyume.Game.Trigger
{
    public class StageExit : MonoBehaviour
    {
        public bool Sleep;

        private void Awake()
        {
            Event.OnStageClear.AddListener(OnStageClear);
        }

        private void OnStageClear()
        {
            StartCoroutine(WaitStageExit());
        }

        private IEnumerator WaitStageExit()
        {
            var cam = Camera.main.gameObject.GetComponent<ActionCamera>();
            cam.target = null;

            var stage = GetComponentInParent<Stage>();
            var player = stage.GetComponentInChildren<Player>();
            var id = stage.Id + 1;
            MoveManager.Instance.HackAndSlash(player, id);

            yield return new WaitForSeconds(1.0f);
            if (Sleep)
                Event.OnPlayerSleep.Invoke();
            else
                Event.OnStageEnter.Invoke();
            Sleep = false;
        }
    }
}
