using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Prologue : MonoBehaviour
    {
        private Player _player;
        private PrologueCharacter _fenrir;
        private PrologueCharacter _fox;
        private PrologueCharacter _pig;
        private PrologueCharacter _knight;
        private UI.Battle _battle;

        public void StartPrologue()
        {
            StartCoroutine(CoStartPrologue());
        }

        private IEnumerator CoStartPrologue()
        {
            Game.instance.Stage.LoadBackground("Chapter_04_03");
            var go = PlayerFactory.Create();
            _player = go.GetComponent<Player>();
            var position = _player.transform.position;
            position.y = Stage.StageStartPosition;
            _player.transform.position = position;
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);
            _battle = Widget.Find<UI.Battle>();
            _battle.ShowForTutorial();
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
            yield return StartCoroutine(CoBattle());
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

        public static void PopupDmg(int damage, GameObject target, bool isPlayer, bool critical)
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

        private IEnumerator PlayerAttack(int damage, PrologueCharacter enemy, bool critical, bool dead)
        {
            _player.Animator.Attack();
            yield return new WaitForSeconds(0.3f);
            PopupDmg(damage, enemy.gameObject, true, critical);
            if (dead)
            {
                enemy.Animator.Die();
            }
            yield return new WaitUntil(() => _player.Animator.IsIdle());
            _battle.ShowComboText(true);
        }

        private IEnumerator PlayerFinisher()
        {
            _player.Animator.Cast();
            yield return new WaitForSeconds(0.3f);
            var effect = Game.instance.Stage.SkillController.Get<SkillAreaVFX>(_knight.gameObject, ElementalType.Fire, SkillCategory.AreaAttack, SkillTargetType.Enemies);
            effect.Play();
            yield return new WaitForSeconds(0.5f);
            var dmgMap = new[] {1600, 4800, 8400, 12000, 34000};
            for (var i = 0; i < 5; i++)
            {
                yield return new WaitForSeconds(0.14f);
                if (i == 4)
                {
                    effect.StopLoop();
                    yield return new WaitForSeconds(0.1f);
                    _player.Animator.CriticalAttack();
                    effect.Finisher();
                    yield return new WaitUntil(() => effect.last.isStopped);
                }
                _battle.ShowComboText(true);
                PopupDmg(dmgMap[i], _knight.gameObject, true, i == 4);
            }
            _pig.Animator.Die();
            _knight.Animator.Die();
            yield return new WaitUntil(() => _player.Animator.IsIdle());
            yield return new WaitForSeconds(1f);
        }

        private IEnumerator CoPlayerHeal()
        {
            _player.Animator.Cast();
            yield return new WaitForSeconds(0.3f);
            var position = _player.transform.TransformPoint(0f, 1.7f, 0f);
            var force = new Vector3(-0.1f, 0.5f);
            DamageText.Show(position, force, 64000.ToString(), DamageText.TextGroupState.Heal);
            VFXController.instance.CreateAndChase<BattleHeal01VFX>(_player.transform, _player.Animator.GetHUDPosition() - new Vector3(0f, 0.4f));
            _player.Animator.Idle();
            yield return new WaitForSeconds(1f);
        }
        private IEnumerator CoBattle()
        {
            var buffRow = Game.instance.TableSheets.BuffSheet.Values.First(r =>
                r.StatModifier.Value < 0 && r.StatModifier.StatType == StatType.DEF);
            yield return StartCoroutine(_fenrir.CoBuff(new DefenseBuff(buffRow), _player.gameObject));
            yield return StartCoroutine(PlayerAttack(1500, _fox, false, false));
            yield return StartCoroutine(_pig.CoNormalAttack(12000, true, _player.gameObject));
            yield return StartCoroutine(PlayerAttack(4500, _fox, true, false));
            yield return StartCoroutine(_fox.CoDoubleAttack(_player.gameObject, new[] {7000, 14000},
                new[] {false, true}));
            yield return StartCoroutine(PlayerAttack(5000, _fox, false, false));
            yield return StartCoroutine(_knight.CoBlowAttack(ElementalType.Water, _player.gameObject));
            yield return StartCoroutine(PlayerAttack(6500, _fox, false, false));
            yield return StartCoroutine(_fox.CoNormalAttack(4000, false, _player.gameObject));
            yield return StartCoroutine(PlayerAttack(18000, _fox, true, true));
            yield return StartCoroutine(_pig.CoNormalAttack(6000, true, _player.gameObject));
            yield return StartCoroutine(PlayerFinisher());
            yield return StartCoroutine(CoPlayerHeal());
            yield return StartCoroutine(PlayerAttack(10500, _fenrir, false, false));
            yield return StartCoroutine(PlayerAttack(30000, _fenrir, true, false));
            yield return StartCoroutine(PlayerAttack(85000, _fenrir, true, false));
            yield return StartCoroutine(CoFenrirFinisher());
        }

        private IEnumerator CoFenrirFinisher()
        {
            Widget.Find<Dialog>().Show();
            yield return new WaitWhile(() => Widget.Find<Dialog>().isActiveAndEnabled);
            yield return StartCoroutine(_fenrir.CoDoubleAttack(_player.gameObject, new[] {36000, 144000},
                new[] {true, true}));
            _player.Animator.Die();
        }
    }
}
