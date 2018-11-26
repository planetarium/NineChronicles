using System.Collections;
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
            yield return new WaitForSeconds(1.0f);
            Event.OnStageEnter.Invoke();
        }
    }
}
