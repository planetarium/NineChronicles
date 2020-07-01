using System.Collections;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Prologue : MonoBehaviour
    {
        public void StartPrologue()
        {
            StartCoroutine(CoStartPrologue());
        }

        private IEnumerator CoStartPrologue()
        {
            Game.instance.Stage.LoadBackground("PVP");
            var go = PlayerFactory.Create();
            var player = go.GetComponent<Player>();
            var position = player.transform.position;
            position.y = Stage.StageStartPosition;
            player.transform.position = position;
            player.StartRun();
            ActionCamera.instance.ChaseX(player.transform);
            Widget.Find<Dialog>().Show();
            yield return new WaitWhile(() => Widget.Find<Dialog>().isActiveAndEnabled);
            Game.instance.Stage.objectPool.ReleaseAll();
            Widget.Find<Synopsis>().part1Ended = true;
            Widget.Find<Synopsis>().Show();
        }
    }
}
