using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Character;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Trigger;
using Nekoyume.Model;
using Nekoyume.Model.Skill;
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
            var player = go.GetComponent<Character.Player>();
            var position = player.transform.position;
            position.y = Stage.StageStartPosition;
            player.transform.position = position;
            player.StartRun();
            ActionCamera.instance.ChaseX(player.transform);
            yield return new WaitForSeconds(2f);
            var go2 = EnemyFactory.Create(205007, player.transform.position);
            var fenrir = go2.GetComponent<PrologueCharacter>();
            yield return new WaitUntil(() => 6f > Mathf.Abs(go.transform.position.x - go2.transform.position.x));
            player.StopRun();
            fenrir.Animator.StandingToIdle();
            yield return new WaitUntil(() => fenrir.Animator.IsIdle());
            yield return StartCoroutine(CoPrologueEnd());
        }

        private IEnumerator CoPrologueEnd()
        {
            Widget.Find<Dialog>().Show();
            yield return new WaitWhile(() => Widget.Find<Dialog>().isActiveAndEnabled);
            ActionCamera.instance.Idle();
            Game.instance.Stage.objectPool.ReleaseAll();
            Widget.Find<Synopsis>().part1Ended = true;
            Widget.Find<Synopsis>().Show();
        }
    }
}
