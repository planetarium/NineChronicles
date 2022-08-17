using Nekoyume.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Elemental;
using System.Linq;
using System;

namespace Nekoyume.Game.Character
{
    using UniRx;

    public class RaidCharacter : Character
    {
        private static Vector3 DamageTextForce => new(-0.1f, 0.5f);
        protected const float AnimatorTimeScale = 1.2f;

        protected Model.CharacterBase _characterModel;
        protected HudContainer _hudContainer;
        protected SpeechBubble _speechBubble;

        protected int _currentHp;

        protected RaidBattle _raidBattle;
        protected float _attackTime;
        protected float _criticalAttackTime;
        protected RaidCharacter _target;
        public bool IsDead => _currentHp <= 0;
        public Model.CharacterBase Model => _characterModel;
        public Coroutine CurrentAction { get; set; }

        protected virtual void Awake()
        {
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
        }

        private void LateUpdate()
        {
            _hudContainer.UpdatePosition(gameObject, HUDOffset);
            _speechBubble.UpdatePosition(gameObject, HUDOffset);
        }

        public virtual void Init(RaidCharacter target)
        {
            gameObject.SetActive(true);

            _raidBattle ??= Widget.Find<RaidBattle>();
            _hudContainer ??= Widget.Create<HudContainer>(true);
            _speechBubble ??= Widget.Create<SpeechBubble>();
            _target = target;
        }

        public void Spawn(Model.CharacterBase model)
        {
            _characterModel = model;
            Id = _characterModel.Id;
            SizeType = _characterModel.SizeType;
            _currentHp = _characterModel.HP;
            UpdateStatusUI();
        }

        public virtual void UpdateStatusUI()
        {
            if (!Game.instance.IsInWorld)
                return;

            _hudContainer.UpdatePosition(gameObject, HUDOffset);
        }

        public IEnumerator CoNormalAttack(IReadOnlyList<Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null || skillInfos.Count == 0)
            {
                yield break;
            }

            ActionPoint = () => ApplyDamage(skillInfos);
            yield return StartCoroutine(CoAnimationAttack(skillInfos.Any(x => x.Critical)));
        }

        public IEnumerator CoBlowAttack(IReadOnlyList<Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosCount = skillInfos.Count;

            if (skillInfos.First().SkillTargetType == Nekoyume.Model.Skill.SkillTargetType.Enemy)
            {
                yield return StartCoroutine(CoAnimationCast(skillInfos.First()));
                yield return StartCoroutine(
                    CoAnimationCastAttack(skillInfos.Any(skillInfo => skillInfo.Critical)));
            }
            else
            {
                yield return StartCoroutine(CoAnimationCastBlow(skillInfos));
            }

            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                var target = info.Target.Id == Id ? this : _target;
                if (target == null)
                    continue;

                var effect = Game.instance.Stage.SkillController.Get<SkillBlowVFX>(target, info);
                if (effect is null)
                    continue;

                effect.Play();
                ProcessAttack(target, info, true);
            }
        }

        public IEnumerator CoDoubleAttack(IReadOnlyList<Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null || skillInfos.Count == 0)
            {
                yield break;
            }

            var skillInfosFirst = skillInfos.First();
            var skillInfosCount = skillInfos.Count;
            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                yield return StartCoroutine(CoAnimationAttack(info.Critical));

                var target = info.Target.Id == Id ? this : _target;
                var effect = Game.instance.RaidStage.SkillController.Get<SkillDoubleVFX>(target, info);
                if (effect != null)
                {
                    if (skillInfosFirst == info)
                    {
                        effect.FirstStrike();
                    }
                    else
                    {
                        effect.SecondStrike();
                    }
                }

                ProcessAttack(target, info, true);
            }
        }

        public IEnumerator CoAreaAttack(IReadOnlyList<Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosFirst = skillInfos.First();
            var skillInfosCount = skillInfos.Count;

            ShowCutscene();
            yield return StartCoroutine(CoAnimationCast(skillInfosFirst));

            var effectTarget = skillInfosFirst.Target.Id == Id ? this : _target;
            var effect = Game.instance.RaidStage.SkillController.Get<SkillAreaVFX>(effectTarget,
                    skillInfosFirst);
            if (effect is null)
                yield break;

            Skill.SkillInfo trigger = null;
            if (effect.finisher)
            {
                var count = FindObjectsOfType(effectTarget.GetType()).Length;
                trigger = skillInfos.Skip(skillInfosCount - count).First();
            }

            effect.Play();
            yield return new WaitForSeconds(0.5f);

            var isTriggerOn = false;
            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                var target = info.Target.Id == Id ? this : _target;

                yield return new WaitForSeconds(0.14f);
                if (trigger == info)
                {
                    isTriggerOn = true;

                    if (!info.Critical)
                    {
                        yield return new WaitForSeconds(0.2f);
                    }

                    if (info.ElementalType == ElementalType.Fire)
                    {
                        effect.StopLoop();
                        yield return new WaitForSeconds(0.1f);
                    }

                    var coroutine = StartCoroutine(CoAnimationCastAttack(info.Critical));
                    if (info.ElementalType == ElementalType.Water)
                    {
                        yield return new WaitForSeconds(0.1f);
                        effect.StopLoop();
                    }

                    yield return coroutine;
                    effect.Finisher();
                    ProcessAttack(target, info, true);
                    if (info.ElementalType != ElementalType.Fire
                        && info.ElementalType != ElementalType.Water)
                    {
                        effect.StopLoop();
                    }

                    yield return new WaitUntil(() => effect.last.isStopped);
                }
                else
                {
                    ProcessAttack(target, info, isTriggerOn);
                }
            }

            yield return new WaitForSeconds(0.5f);
        }

        public IEnumerator CoBuff(IReadOnlyList<Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            yield return StartCoroutine(CoAnimationBuffCast(skillInfos.First()));

            foreach (var info in skillInfos)
            {
                var target = info.Target.Id == Id ? this : _target;
                target.ProcessBuff(target, info);
            }

            Animator.Idle();
        }

        public IEnumerator CoHeal(IReadOnlyList<Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null || skillInfos.Count == 0)
                yield break;

            yield return StartCoroutine(CoAnimationCast(skillInfos.First()));

            foreach (var info in skillInfos)
            {
                if (info.Target.Id != Id)
                {
                    Debug.LogWarning($"[{nameof(RaidCharacter)}] Heal target is different from expected.");
                }

                ProcessHeal(info);
            }

            Animator.Idle();
        }

        protected virtual void ShowCutscene()
        {
        }

        private IEnumerator CoAnimationAttack(bool isCritical)
        {
            if (isCritical)
            {
                Animator.CriticalAttack();
            }
            else
            {
                Animator.Attack();
            }

            yield return new WaitForSeconds(isCritical ? _criticalAttackTime : _attackTime);
        }

        private IEnumerator CoAnimationCastAttack(bool isCritical)
        {
            if (isCritical)
            {
                Animator.CriticalAttack();
            }
            else
            {
                Animator.CastAttack();
            }

            yield return null;
        }

        private IEnumerator CoAnimationCastBlow(IReadOnlyList<Skill.SkillInfo> infos)
        {
            var info = infos.First();
            var copy = new Skill.SkillInfo(info.Target, info.Effect,
                info.Critical, info.SkillCategory,
                info.WaveTurn, ElementalType.Normal, info.SkillTargetType, info.Buff);
            yield return StartCoroutine(CoAnimationCast(copy));

            var pos = transform.position;
            yield return CoAnimationCastAttack(infos.Any(skillInfo => skillInfo.Critical));
            var effect = Game.instance.RaidStage.SkillController.GetBlowCasting(
                pos,
                info.SkillCategory,
                info.ElementalType);
            effect.Play();
            yield return new WaitForSeconds(0.2f);
        }

        protected virtual IEnumerator CoAnimationCast(Skill.SkillInfo info)
        {
            ShowSpeech("PLAYER_SKILL", (int)info.ElementalType, (int)info.SkillCategory);
            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.RaidStage.SkillController.Get(pos, info.ElementalType);
            effect.Play();
            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator CoAnimationBuffCast(Skill.SkillInfo info)
        {
            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.RaidStage.BuffController.Get(pos, info.Buff);
            effect.Play();
            yield return new WaitForSeconds(0.6f);
        }

        public void ShowSpeech(string key, params int[] list)
        {
            _speechBubble.enable = true;

            if (_speechBubble.gameObject.activeSelf)
            {
                return;
            }

            if (list.Length > 0)
            {
                var join = string.Join("_", list.Select(x => x.ToString()).ToArray());
                key = $"{key}_{join}_";
            }
            else
            {
                key = $"{key}_";
            }

            if (!_speechBubble.SetKey(key))
            {
                return;
            }

            if (!gameObject.activeSelf)
                return;

            StartCoroutine(_speechBubble.CoShowText());
        }

        private void PopUpDmg(
            Vector3 position,
            Vector3 force,
            Skill.SkillInfo info,
            bool isConsiderElementalType)
        {
            var dmg = info.Effect.ToString();
            var pos = transform.position;
            pos.x -= 0.2f;
            pos.y += 0.32f;
            var group = _characterModel is Model.Player
                ? DamageText.TextGroupState.Damage
                : DamageText.TextGroupState.Basic;

            if (info.Critical)
            {
                ActionCamera.instance.Shake();
                AudioController.PlayDamagedCritical();
                CriticalText.Show(position, force, dmg, group);
                if (info.SkillCategory == Nekoyume.Model.Skill.SkillCategory.NormalAttack)
                    VFXController.instance.Create<BattleAttackCritical01VFX>(pos);
            }
            else
            {
                AudioController.PlayDamaged(isConsiderElementalType
                    ? info.ElementalType
                    : ElementalType.Normal);
                DamageText.Show(position, force, dmg, group);
                if (info.SkillCategory == Nekoyume.Model.Skill.SkillCategory.NormalAttack)
                    VFXController.instance.Create<BattleAttack01VFX>(pos);
            }
        }

        private void ApplyDamage(IReadOnlyList<Skill.SkillInfo> skillInfos)
        {
            for (var i = 0; i < skillInfos.Count; i++)
            {
                var info = skillInfos[i];
                var target = info.Target.Id == Id ? this : _target;
                ProcessAttack(target, info, false);
                if (info.Target is Model.Enemy)
                {
                    _raidBattle.ShowComboText(info.Effect > 0);
                }
            }
        }

        private RaidCharacter GetTarget(Skill.SkillInfo info)
        {
            return info.Target.Id == Id ? this : _target;
        }

        private void ProcessAttack(RaidCharacter target, Skill.SkillInfo skill, bool isConsiderElementalType)
        {
            ShowSpeech("PLAYER_SKILL", (int)skill.ElementalType, (int)skill.SkillCategory);
            StartCoroutine(target.CoProcessDamage(skill, isConsiderElementalType));
            ShowSpeech("PLAYER_ATTACK");
        }

        private void ProcessBuff(RaidCharacter target, Skill.SkillInfo info)
        {
            if (IsDead)
            {
                return;
            }

            var buff = info.Buff;
            var effect = Game.instance.RaidStage.BuffController.Get<RaidCharacter, BuffVFX>(target, buff);
            effect.Play();
        }

        private void ProcessHeal(Skill.SkillInfo info)
        {
            if (IsDead)
            {
                return;
            }

            _currentHp = Math.Min(_currentHp + info.Effect, _characterModel.HP);

            var position = transform.TransformPoint(0f, 1.7f, 0f);
            var force = new Vector3(-0.1f, 0.5f);
            var txt = info.Effect.ToString();
            DamageText.Show(position, force, txt, DamageText.TextGroupState.Heal);
            VFXController.instance.CreateAndChase<BattleHeal01VFX>(transform, HealOffset);
        }

        private IEnumerator CoProcessDamage(Skill.SkillInfo info, bool isConsiderElementalType)
        {
            var dmg = info.Effect;

            var position = transform.TransformPoint(0f, 1.7f, 0f);
            var force = DamageTextForce;

            if (dmg <= 0)
            {
                var index = _characterModel is Model.Player ? 1 : 0;
                MissText.Show(position, force, index);
                yield break;
            }

            var value = _currentHp - dmg;
            _currentHp = Mathf.Clamp(value, 0, _characterModel.HP);

            Animator.Hit();
            UpdateStatusUI();
            PopUpDmg(position, force, info, isConsiderElementalType);
        }

        public virtual IEnumerator CoDie()
        {
            OnDeadStart();
            yield return new WaitForSeconds(0.8f);
            OnDeadEnd();
        }

        protected virtual void OnDeadStart()
        {
            ShowSpeech("PLAYER_LOSE");
            Animator.Die();
        }

        protected virtual void OnDeadEnd()
        {
            Animator.Idle();
            if (this is RaidPlayer)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
