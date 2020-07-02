using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.Game.VFX;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Prologue : MonoBehaviour
    {
        private Character.Player _player;
        private PrologueCharacter _fenrir;
        private PrologueCharacter _fox;
        private PrologueCharacter _pig;
        private PrologueCharacter _knight;

        public void StartPrologue()
        {
            StartCoroutine(CoStartPrologue());
        }

        private IEnumerator CoStartPrologue()
        {
            Game.instance.Stage.LoadBackground("PVP");
            var go = PlayerFactory.Create();
            _player = go.GetComponent<Player>();
            var position = _player.transform.position;
            position.y = Stage.StageStartPosition;
            _player.transform.position = position;
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);
            yield return new WaitForSeconds(2f);
            var go2 = EnemyFactory.Create(205007, _player.transform.position, 10f);
            _fenrir = go2.GetComponent<PrologueCharacter>();
            yield return new WaitUntil(() => 6f > Mathf.Abs(go.transform.position.x - go2.transform.position.x));
            _player.StopRun();
            _fenrir.Animator.StandingToIdle();
            yield return new WaitUntil(() => _fenrir.Animator.IsIdle());
            Widget.Find<Dialog>().Show();
            yield return new WaitWhile(() => Widget.Find<Dialog>().isActiveAndEnabled);
            yield return StartCoroutine(CoSpawnWave(go));
            _fenrir.Animator.CastAttack();
            yield return new WaitUntil(() => _fenrir.Animator.IsIdle());
            _player.Animator.Attack();
            yield return new WaitForSeconds(0.3f);
            PopupDmg(100, _fox.gameObject, true, false);
            _knight.Animator.Cast();
            yield return new WaitUntil(() => _player.Animator.IsIdle());
            yield return StartCoroutine(_pig.CoNormalAttack(true));
            PopupDmg(100, _player.gameObject, false, true);
            yield return StartCoroutine(_fox.CoNormalAttack(false));
            PopupDmg(100, _player.gameObject, false, false);
            yield return StartCoroutine(_fox.CoNormalAttack(true));
            PopupDmg(100, _player.gameObject, false, true);
            yield return StartCoroutine(_knight.CoNormalAttack(true));
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

        private IEnumerator CoSpawnWave(GameObject player)
        {
            var monsterIds = new List<int>
            {
                202002,
                203005,
                205000,
            };
            yield return StartCoroutine(
                Game.instance.Stage.spawner.CoSpawnWave(monsterIds, player.transform.position, 0f));
            var characters = GetComponentsInChildren<PrologueCharacter>();
            _fox = characters[1];
            _pig = characters[2];
            _knight = characters[3];
        }

        private void PopupDmg(int damage, GameObject target, bool isPlayer, bool critical)
        {
            var dmg = damage.ToString();
            var pos = target.transform.position;
            pos.x -= 0.2f;
            pos.y += 0.32f;
            Vector3 position;
            Vector3 force;
            if (isPlayer)
            {
                force = new Vector3(-0.1f, 0.5f);
                position = target.transform.TransformPoint(0f, 1.7f, 0f);
            }
            else
            {
                force = new Vector3(0f, 0.8f);
                position = target.transform.TransformPoint(0f, 1f, 0f);
            }
            var group = isPlayer ? DamageText.TextGroupState.Damage : DamageText.TextGroupState.Basic;
            if (critical)
            {
                ActionCamera.instance.Shake();
                AudioController.PlayDamagedCritical();
                CriticalText.Show(position, force, dmg, group);
                VFXController.instance.Create<BattleAttackCritical01VFX>(pos);
            }
            else
            {
                AudioController.PlayDamaged();
                DamageText.Show(position, force, dmg, group);
                VFXController.instance.Create<BattleAttack01VFX>(pos);
            }
        }
    }
}
