using System;
using System.Collections;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;
using UniRx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    using UniRx;

    [Obsolete]
    public class PrologueCharacter : MonoBehaviour
    {
        public bool AttackEndCalled { get; set; }
        public CharacterAnimator Animator { get; protected set; }
        public string TargetTag { get; protected set; }
        private CharacterSpineController SpineController { get; set; }
        private bool _forceQuit;
        private Player _target;

        private void Awake()
        {
            Animator = new EnemyAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = 1.2f;

            TargetTag = Tag.Player;
        }

        public void Set(int characterId, Player target)
        {
            var key = characterId.ToString();
            if (Animator.Target != null)
            {
                if (Animator.Target.name.Contains(key))
                    return;

                Animator.DestroyTarget();
            }

            var go = ResourceManager.Instance.Instantiate(key, gameObject.transform);
            if (go == null)
            {
                NcDebug.LogError($"Missing Spine Resource: {key}");
                return;
            }

            SpineController = go.GetComponent<CharacterSpineController>();
            Animator.ResetTarget(go);
            if (characterId == 205007)
            {
                Animator.Standing();
            }
            else
            {
                Animator.Idle();
            }
            _target = target;
        }

        private void OnAnimatorEvent(string eventName)
        {
            switch (eventName)
            {
                case "attackStart":
                    AudioController.PlaySwing();
                    break;
                case "attackPoint":
                    AttackEndCalled = true;
                    break;
                case "footstep":
                    AudioController.PlayFootStep();
                    break;
            }
        }

        public IEnumerator CoNormalAttack(int dmg, bool critical)
        {
            yield return StartCoroutine(CoAnimationAttack(critical));
            Prologue.PopupDmg(dmg, _target.gameObject, false, critical, ElementalType.Normal, false);
            _target.Animator.Hit();
        }

        private IEnumerator CoAnimationAttack(bool isCritical)
        {
            while (true)
            {
                AttackEndCalled = false;
                if (isCritical)
                {
                    Animator.CriticalAttack();
                }
                else
                {
                    Animator.Attack();
                }

                _forceQuit = false;
                var coroutine = StartCoroutine(CoTimeOut());
                yield return new WaitUntil(() => AttackEndCalled || _forceQuit);
                StopCoroutine(coroutine);
                if (_forceQuit)
                {
                    continue;
                }

                break;
            }
        }

        private IEnumerator CoTimeOut()
        {
            yield return new WaitForSeconds(1f);
            _forceQuit = true;
        }

        public IEnumerator CoBlowAttack(ElementalType elementalType)
        {
            AttackEndCalled = false;
            yield return StartCoroutine(CoAnimationCast(elementalType));

            yield return StartCoroutine(CoAnimationCastBlow(elementalType));

            var dmgMap = new[] {1374, 2748, 4122, 8244, 16488};
            var effect = Game.instance.Stage.SkillController.Get<SkillBlowVFX>(_target.gameObject, elementalType, SkillCategory.BlowAttack, SkillTargetType.Enemies);
            effect.Play();
            for (var i = 0; i < 5; i++)
            {
                var sec = i == 0 ? 0 : i / 10f;
                Prologue.PopupDmg(dmgMap[i], _target.gameObject, false, i == 4, elementalType, false);
                _target.Animator.Hit();
                yield return new WaitForSeconds(sec);
            }
        }

        protected virtual IEnumerator CoAnimationCast(ElementalType elementalType)
        {
            var sfxCode = AudioController.GetElementalCastingSFX(elementalType);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.Stage.SkillController.Get(pos, elementalType);
            effect.Play();
            yield return new WaitForSeconds(Game.DefaultSkillDelay);
        }

        private IEnumerator CoAnimationCastBlow(ElementalType elementalType)
        {
            yield return StartCoroutine(CoAnimationCast(elementalType));

            var pos = transform.position;
            yield return CoAnimationCastAttack(false);
            var effect = Game.instance.Stage.SkillController.GetBlowCasting(
                pos,
                SkillCategory.BlowAttack,
                elementalType);
            effect.Play();
            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator CoAnimationCastAttack(bool isCritical)
        {
            while (true)
            {
                AttackEndCalled = false;
                if (isCritical)
                {
                    Animator.CriticalAttack();
                }
                else
                {
                    Animator.CastAttack();
                }

                _forceQuit = false;
                var coroutine = StartCoroutine(CoTimeOut());
                yield return new WaitUntil(() => AttackEndCalled || _forceQuit);
                StopCoroutine(coroutine);
                if (_forceQuit)
                {
                    continue;
                    ;
                }

                break;
            }
        }

        public IEnumerator CoDoubleAttack(int[] damageMap, bool[] criticalMap)
        {
            var go = _target.gameObject;
            var effect = Game.instance.Stage.SkillController.Get<SkillDoubleVFX>(go, ElementalType.Fire, SkillCategory.DoubleAttack, SkillTargetType.Enemy);
            for (var i = 0; i < 2; i++)
            {
                var first = i == 0;

                yield return StartCoroutine(CoAnimationAttack(!first));
                if (first)
                {
                    effect.FirstStrike();
                }
                else
                {
                    effect.SecondStrike();
                }
                Prologue.PopupDmg(damageMap[i], go, false, criticalMap[i], ElementalType.Fire, false);
                _target.Animator.Hit();
                yield return new WaitUntil(() => _target.Animator.IsIdle());
            }
        }

        public IEnumerator CoBuff(StatBuff buff)
        {
            yield return StartCoroutine(CoAnimationBuffCast(buff));
            Animator.CastAttack();
            AudioController.instance.PlaySfx(AudioController.SfxCode.FenrirGrowlCastingAttack);
            var effect = Game.instance.Stage.BuffController.Get<BuffVFX>(_target.gameObject, buff);
            effect.Play();
            yield return new WaitForSeconds(Game.DefaultSkillDelay);
        }

        private IEnumerator CoAnimationBuffCast(StatBuff buff)
        {
            AttackEndCalled = false;
            var sfxCode = AudioController.GetElementalCastingSFX(ElementalType.Normal);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.Stage.BuffController.Get(pos, buff);
            effect.Play();
            yield return new WaitForSeconds(Game.DefaultSkillDelay);
        }

        public IEnumerator CoFinisher(int[] damageMap, bool[] criticalMap)
        {
            AttackEndCalled = false;
            var position = ActionCamera.instance.Cam.ScreenToWorldPoint(
                new Vector2((float) Screen.width / 2, (float) Screen.height / 2));
            position.z = 0f;
            var effect = Game.instance.Stage.objectPool.Get<FenrirSkillVFX>(position);
            effect.Stop();
            AudioController.instance.PlaySfx(AudioController.SfxCode.FenrirGrowlSkill);
            yield return new WaitForSeconds(2f);
            Animator.Skill();
            ActionCamera.instance.Shake();
            var time = Time.time;
            yield return new WaitUntil(() => AttackEndCalled || Time.time - time > 1f);
            for (var i = 0; i < 2; i++)
            {
                var first = i == 0;
                if (first)
                {
                    effect.Play();
                }
                else
                {
                    Time.timeScale = 0.4f;
                }
                Prologue.PopupDmg(damageMap[i], _target.gameObject, false, criticalMap[i], ElementalType.Normal, false);
                _target.Animator.Hit();
                if (first)
                {
                    yield return new WaitForSeconds(0.3f);
                }
                else
                {
                    _target.Animator.Die();
                }
            }
        }

        public IEnumerator CoHit()
        {
            Animator.Hit();
            yield return new WaitUntil(() => Animator.IsIdle());
        }
    }
}
