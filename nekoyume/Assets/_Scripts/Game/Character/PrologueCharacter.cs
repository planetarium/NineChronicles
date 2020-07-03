using System;
using System.Collections;
using System.Linq;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model.Buff;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Skill;
using UniRx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class PrologueCharacter : MonoBehaviour
    {
        public bool AttackEndCalled { get; set; }
        public CharacterAnimator Animator { get; protected set; }
        public string TargetTag { get; protected set; }
        private CharacterSpineController SpineController { get; set; }
        private bool _forceQuit;

        private void Awake()
        {
            Animator = new EnemyAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = 1.2f;

            TargetTag = Tag.Player;
        }

        public void Set(int characterId)
        {
            var spineResourcePath = $"Character/Monster/{characterId}";

            if (!(Animator.Target is null))
            {
                var animatorTargetName = spineResourcePath.Split('/').Last();
                if (Animator.Target.name.Contains(animatorTargetName))
                    return;

                Animator.DestroyTarget();
            }

            var origin = Resources.Load<GameObject>(spineResourcePath);
            var go = Instantiate(origin, gameObject.transform);
            SpineController = go.GetComponent<CharacterSpineController>();
            Animator.ResetTarget(go);
            Animator.Standing();
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

        public IEnumerator CoNormalAttack(int dmg, bool critical, GameObject target)
        {
            yield return StartCoroutine(CoAnimationAttack(critical));
            Prologue.PopupDmg(dmg, target, false, critical);
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

        public IEnumerator CoBlowAttack(ElementalType elementalType, GameObject target)
        {
            AttackEndCalled = false;
            yield return StartCoroutine(CoAnimationCast(elementalType));

            yield return StartCoroutine(CoAnimationCastBlow(elementalType));

            var dmgMap = new[] {1000, 2000, 4000, 8000, 16000};
            var effect = Game.instance.Stage.SkillController.Get<SkillBlowVFX>(target, elementalType, SkillCategory.BlowAttack, SkillTargetType.Enemies);
            effect.Play();
            for (var i = 0; i < 5; i++)
            {
                var sec = i == 0 ? 0 : i / 10f;
                Prologue.PopupDmg(dmgMap[i], target, false, false);
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
            yield return new WaitForSeconds(0.6f);
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

        public IEnumerator CoDoubleAttack(GameObject target, int[] damageMap, bool[] criticalMap)
        {
            var effect = Game.instance.Stage.SkillController.Get<SkillDoubleVFX>(target, ElementalType.Fire, SkillCategory.DoubleAttack, SkillTargetType.Enemy);
            for (var i = 0; i < 2; i++)
            {
                if (target is null)
                    continue;

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
                Prologue.PopupDmg(damageMap[i], target, false, criticalMap[i]);
            }
        }

        public IEnumerator CoBuff(Buff buff, GameObject target)
        {
            yield return StartCoroutine(CoAnimationBuffCast(buff));
            var effect = Game.instance.Stage.BuffController.Get<BuffVFX>(target, buff);
            effect.Play();
            Animator.Idle();
            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator CoAnimationBuffCast(Buff buff)
        {
            AttackEndCalled = false;
            var sfxCode = AudioController.GetElementalCastingSFX(ElementalType.Normal);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.Stage.BuffController.Get(pos, buff);
            effect.Play();
            yield return new WaitForSeconds(0.6f);
        }
    }
}
