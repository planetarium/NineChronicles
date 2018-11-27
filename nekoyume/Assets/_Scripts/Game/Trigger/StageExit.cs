using System.Collections;
using Nekoyume.Game.Character;
using Nekoyume.Move;
using UnityEngine;


namespace Nekoyume.Game.Trigger
{
    public class StageExit : MonoBehaviour
    {
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

            yield return new WaitForSeconds(1.0f);
            Event.OnStageEnter.Invoke();

            var stage = GetComponentInParent<Stage>();
            var player = stage.GetComponentInChildren<Player>();
            var id = 1;
            if (stage.Id < 3) id = stage.Id + 1;
            MoveManager.Instance.HackAndSlash(player, id);
        }
    }
}
