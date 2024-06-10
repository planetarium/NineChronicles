using System;
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
using Nekoyume.UI;
using UnityEngine;
using mixpanel;
using Nekoyume.Game.Battle;
using Nekoyume.UI.Module;

namespace Nekoyume.Game
{
    [Obsolete]
    public class Prologue : MonoBehaviour
    {
        private Player _player;
        private PrologueCharacter _fenrir;
        private PrologueCharacter _fox;
        private PrologueCharacter _pig;
        private PrologueCharacter _knight;
        private UI.Battle _battle;
        private int _armorId = 10251001;
        private int _weaponId = 10151000;
        private int _characterId = 205007;

        public void StartPrologue()
        {
            StartCoroutine(CoStartPrologue());
        }

        private IEnumerator CoStartPrologue()
        {
            Analyzer.Instance.Track("Unity/Prologuebattle Start", new Dictionary<string, Value>()
            {
                ["AgentAddress"] = Game.instance.States.AgentState.address.ToString(),
            });

            var evt = new AirbridgeEvent("Prologue_Battle_Start");
            evt.AddCustomAttribute("agent-address", Game.instance.States.AgentState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            StartCoroutine(Widget.Find<Blind>().FadeOut(2f));
            ActionCamera.instance.InPrologue = true;
            AudioController.instance.PlayMusic(AudioController.MusicCode.PrologueBattle);
            Game.instance.Stage.LoadBackground("Chapter_Prologue");
            var go = PlayerFactory.Create();
            _player = go.GetComponent<Player>();
            _player.EquipForPrologue(_armorId, _weaponId);
            var position = _player.transform.position;
            position.y = Stage.StageStartPosition;
            _player.transform.position = position;
            _player.StartRun();
            ActionCamera.instance.ChaseX(_player.transform);
            _battle = Widget.Find<UI.Battle>();
            _battle.ShowForTutorial(true);
            Widget.Find<HeaderMenuStatic>().Close(true);
            yield return new WaitForSeconds(2f);
            var go2 = StageMonsterFactory.Create(_characterId, _player.transform.position, 7f, _player);
            _fenrir = go2.GetComponent<PrologueCharacter>();
            yield return new WaitUntil(() => 6f > Mathf.Abs(go.transform.position.x - go2.transform.position.x));
            _player.ShowSpeech("PLAYER_PROLOGUE_SPEECH");
            AudioController.instance.PlaySfx(AudioController.SfxCode.FenrirGrowlCasting);
            _player.StopRun();
            _fenrir.Animator.StandingToIdle();
            yield return new WaitUntil(() => _fenrir.Animator.IsIdle());
            yield return new WaitForSeconds(1f);
            Widget.Find<PrologueDialogPopup>().Show();
            yield return new WaitWhile(() => Widget.Find<PrologueDialogPopup>().isActiveAndEnabled);
            yield return StartCoroutine(CoSpawnWave(go));
            yield return StartCoroutine(CoBattle());
            yield return StartCoroutine(CoPrologueEnd());
        }

        private IEnumerator CoPrologueEnd()
        {
            Widget.Find<Synopsis>().prolgueEnd = true;
            StartCoroutine(Widget.Find<Blind>().FadeIn(2f, ""));
            yield return new WaitForSeconds(2f);
            ActionCamera.instance.Idle();
            Game.instance.Stage.objectPool.ReleaseAll();
            AudioController.instance.StopAll();
            StartCoroutine(Widget.Find<Blind>().FadeOut(2f));
            Game.instance.Stage.LoadBackground("nest");
            Widget.Find<Synopsis>().Show();
            Game.instance.Stage.objectPool.Remove<Player>(_player.gameObject);
            ActionCamera.instance.InPrologue = false;
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
                Game.instance.Stage.spawner.CoSpawnWave(monsterIds, player.transform.position, 0f, _fenrir, _player));
            var characters = GetComponentsInChildren<PrologueCharacter>();
            _fox = characters[1];
            _pig = characters[2];
            _knight = characters[3];
        }

        public static void PopupDmg(int damage, GameObject target, bool isPlayer, bool critical, ElementalType elementalType, bool isFenrir)
        {
            var dmg = damage.ToString();
            var pos = target.transform.position;
            if (isFenrir)
            {
                pos.x -= 2.2f;
            }
            else
            {
                pos.x -= 0.2f;
            }
            pos.y += 0.32f;
            Vector3 position;
            Vector3 force;
            if (isPlayer)
            {
                force = new Vector3(-0.1f, 0.5f);
                var x = isFenrir ? -2f : 0f;
                position = target.transform.TransformPoint(x, 1.7f, 0f);
            }
            else
            {
                force = new Vector3(0f, 0.8f);
                position = target.transform.TransformPoint(0f, 1.7f, 0f);
            }
            var group = !isPlayer ? DamageText.TextGroupState.Damage : DamageText.TextGroupState.Basic;
            if (critical)
            {
                ActionCamera.instance.Shake();
                AudioController.PlayDamagedCritical();
                CriticalText.Show(position, force, dmg, group);
                VFXController.instance.Create<BattleAttackCritical01VFX>(pos);
            }
            else
            {
                AudioController.PlayDamaged(elementalType);
                DamageText.Show(ActionCamera.instance.Cam, position, force, dmg, group);
                VFXController.instance.Create<BattleAttack01VFX>(pos);
            }
        }

        private IEnumerator PlayerAttack(int damage, PrologueCharacter enemy, bool critical, bool dead, bool isFenrir = false)
        {
            _player.Ready();
            if (critical)
            {
                _player.Animator.CriticalAttack();
            }
            else
            {
                _player.Animator.Attack();
            }
            yield return new WaitUntil(() => _player.AttackEnd);
            if (dead)
            {
                enemy.Animator.Die();
            }
            else
            {
                enemy.Animator.Hit();
            }
            _battle.ShowComboText(true);
            PopupDmg(damage, enemy.gameObject, true, critical, ElementalType.Normal, isFenrir);
        }

        private IEnumerator PlayerFinisher()
        {
            _player.Ready();
            var sfxCode = AudioController.GetElementalCastingSFX(ElementalType.Fire);
            AudioController.instance.PlaySfx(sfxCode);
            _player.Animator.Cast();
            var pos = _player.transform.position;
            var castingEffect = Game.instance.Stage.SkillController.Get(pos, ElementalType.Fire);
            castingEffect.Play();
            AreaAttackCutscene.Show(_armorId);
            yield return new WaitForSeconds(Game.DefaultSkillDelay);
            var effect = Game.instance.Stage.SkillController.Get<SkillAreaVFX>(_knight.gameObject, ElementalType.Fire, SkillCategory.AreaAttack, SkillTargetType.Enemies);
            effect.Play();
            yield return new WaitForSeconds(0.5f);
            var dmgMap = new[] {1617, 4851, 8085, 12936, 38808};
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
                    yield return new WaitForSeconds(0.2f);
                }
                _battle.ShowComboText(true);
                PopupDmg(dmgMap[i], _pig.gameObject, true, i == 4, ElementalType.Fire, false);
                PopupDmg(dmgMap[i], _knight.gameObject, true, i == 4, ElementalType.Fire, false);
                StartCoroutine(_pig.CoHit());
                StartCoroutine(_knight.CoHit());
            }
            _pig.Animator.Die();
            _knight.Animator.Die();
            yield return new WaitUntil(() => _player.AttackEnd);
            yield return new WaitForSeconds(1f);
        }

        private IEnumerator CoPlayerHeal()
        {
            _player.Ready();
            _player.Animator.Cast();
            AudioController.instance.PlaySfx(AudioController.SfxCode.Heal);
            var buffRow = Game.instance.TableSheets.StatBuffSheet.Values.First(r =>
                r.Value > 0 && r.StatType == StatType.HP);
            var buff = new StatBuff(buffRow);
            var castingEffect = Game.instance.Stage.BuffController.Get(_player.transform.position, buff);
            castingEffect.Play();
            yield return new WaitForSeconds(Game.DefaultSkillDelay);
            var effect = Game.instance.Stage.BuffController.Get<BuffVFX>(_player.gameObject, buff);
            effect.Play();
            var position = _player.transform.TransformPoint(0f, 1.7f, 0f);
            var force = new Vector3(-0.1f, 0.5f);
            DamageText.Show(ActionCamera.instance.Cam, position, force, 64000.ToString(), DamageText.TextGroupState.Heal);
            yield return new WaitForSeconds(1f);
            _player.Animator.Idle();
            yield return new WaitForSeconds(1f);
        }
        private IEnumerator CoBattle()
        {
            var buffRow = Game.instance.TableSheets.StatBuffSheet.Values.First(r =>
                r.Value < 0 && r.StatType == StatType.DEF);
            yield return StartCoroutine(_fenrir.CoBuff(new StatBuff(buffRow)));
            yield return new WaitForSeconds(0.7f);
            yield return StartCoroutine(PlayerAttack(1524, _fox, true, false));
            yield return StartCoroutine(_pig.CoNormalAttack(12733, true));
            yield return StartCoroutine(PlayerAttack(4518, _fox, true, false));
            yield return StartCoroutine(_fox.CoDoubleAttack(new[] {7126, 14352}, new[] {false, true}));
            yield return StartCoroutine(PlayerAttack(5772, _fox, true, false));
            yield return StartCoroutine(_knight.CoBlowAttack(ElementalType.Water));
            yield return StartCoroutine(PlayerAttack(6502, _fox, true, false));
            yield return StartCoroutine(_fox.CoNormalAttack(4508, true));
            yield return StartCoroutine(PlayerAttack(18701, _fox, true, true));
            yield return StartCoroutine(_pig.CoNormalAttack(6910, true));
            yield return StartCoroutine(PlayerFinisher());
            yield return StartCoroutine(CoPlayerHeal());
            yield return StartCoroutine(PlayerAttack(10897, _fenrir, true, false, true));
            AudioController.instance.PlaySfx(AudioController.SfxCode.FenrirGrowlCastingAttack);
            yield return StartCoroutine(_fenrir.CoNormalAttack(76054, true));
            yield return StartCoroutine(PlayerAttack(48913, _fenrir, true, false, true));
            yield return StartCoroutine(PlayerAttack(89976, _fenrir, true, false, true));
            yield return StartCoroutine(CoFenrirFinisher());
        }

        private IEnumerator CoFenrirFinisher()
        {
            yield return new WaitForSeconds(1f);
            Widget.Find<PrologueDialogPopup>().Show();
            yield return new WaitWhile(() => Widget.Find<PrologueDialogPopup>().isActiveAndEnabled);
            yield return StartCoroutine(_fenrir.CoFinisher(new[] {580214, 999999}, new[] {true, true}));
            yield return new WaitForSeconds(1f);
            Time.timeScale = Game.DefaultTimeScale;
            _fenrir.Animator.Idle();
        }
    }
}
