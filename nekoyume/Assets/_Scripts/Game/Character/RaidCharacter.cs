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
using Cysharp.Threading.Tasks;
using Nekoyume.Game.Battle;
using Nekoyume.Helper;

namespace Nekoyume.Game.Character
{
    using Nekoyume.Model.Buff;
    using UniRx;

    public class RaidCharacter : Character
    {
        private static Vector3 DamageTextForce => new(-0.1f, 0.5f);
        protected const float AnimatorTimeScale = 1.2f;

        protected Model.CharacterBase _characterModel;
        protected HudContainer _hudContainer;
        protected SpeechBubble _speechBubble;
        public HpBar HPBar { get; private set; }

        protected long _currentHp;

        protected WorldBossBattle _worldBossBattle;
        protected float _attackTime;
        protected float _criticalAttackTime;
        protected RaidCharacter _target;
        public bool IsDead => _currentHp <= 0;
        public Model.CharacterBase Model => _characterModel;
        public Coroutine CurrentAction { protected get; set; }
        public Coroutine TargetAction => _target.CurrentAction;
        public bool IsActing => CurrentAction != null;

        private bool _isAppQuitting = false;
        private readonly Dictionary<int, VFX.VFX> _persistingVFXMap = new();
        protected override Vector3 HUDOffset => base.HUDOffset + new Vector3(0f, 0.35f, 0f);

        private readonly List<int> removedBuffVfxList = new();

        protected virtual void Awake()
        {
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
        }

        private void LateUpdate()
        {
            _hudContainer?.UpdatePosition(Game.instance.RaidStage.Camera.Cam, gameObject, HUDOffset);
            _speechBubble?.UpdatePosition(Game.instance.RaidStage.Camera.Cam, gameObject, HUDOffset);
        }

        private void OnDisable()
        {
            foreach (var vfx in _persistingVFXMap.Values)
            {
                vfx.gameObject.SetActive(false);
            }
            _persistingVFXMap.Clear();

            if (!_isAppQuitting)
            {
                DisableHUD();
            }
        }

        private void OnDestroy()
        {
            foreach (var vfx in _persistingVFXMap.Values)
            {
                vfx.transform.parent = Game.instance.Stage.transform;
                vfx.LazyStop();
            }
        }

        private void OnApplicationQuit()
        {
            _isAppQuitting = true;
        }

        public virtual void Init(RaidCharacter target)
        {
            gameObject.SetActive(true);

            _worldBossBattle ??= Widget.Find<WorldBossBattle>();
            _hudContainer ??= Widget.Create<HudContainer>(true);
            _speechBubble ??= Widget.Create<SpeechBubble>();
            _target = target;
        }

        public void Spawn(Model.CharacterBase model)
        {
            Set(model, true);
            Id = _characterModel.Id;
            SizeType = _characterModel.SizeType;
            UpdateStatusUI();
        }

        public virtual void Set(Model.CharacterBase model, bool updateCurrentHP = false)
        {
            _characterModel = model;
            if (updateCurrentHP)
            {
                _currentHp = _characterModel.HP;
            }
        }

        protected virtual void InitializeHpBar()
        {
            HPBar = Widget.Create<HpBar>(true);
            HPBar.transform.SetParent(_hudContainer.transform);
            HPBar.transform.localPosition = Vector3.zero;
            HPBar.transform.localScale = Vector3.one;
        }

        public virtual void UpdateHpBar()
        {
            if (!BattleRenderer.Instance.IsOnBattle)
                return;

            if (!HPBar)
            {
                InitializeHpBar();
                _hudContainer.UpdateAlpha(1);
            }

            _hudContainer.UpdatePosition(Game.instance.RaidStage.Camera.Cam, gameObject, HUDOffset);
            HPBar.Set(_currentHp, _characterModel.AdditionalHP, _characterModel.HP);
            HPBar.SetBuffs(_characterModel.Buffs);

            UpdateBuffVfx();

            HPBar.SetLevel(_characterModel.Level);

            //OnUpdateHPBar.OnNext(this);
        }

        public virtual void UpdateBuffVfx()
        {
            // delete existing vfx
            removedBuffVfxList.Clear();
            foreach (var buff in _persistingVFXMap.Keys)
            {
                if (!Model.IsDead && Model.Buffs.Keys.Contains(buff))
                {
                    continue;
                }
                _persistingVFXMap[buff].LazyStop();
                removedBuffVfxList.Add(buff);
            }

            foreach (var id in removedBuffVfxList)
            {
                _persistingVFXMap.Remove(id);
                OnBuffEnd?.Invoke(id);
            }
        }

        public void DisableHUD()
        {
            // No pooling. HUD Pooling causes HUD positioning bug.
            if (HPBar)
            {
                Destroy(HPBar.gameObject);
                HPBar = null;
            }

            // No pooling. HUD Pooling causes HUD positioning bug.
            if (_hudContainer)
            {
                Destroy(_hudContainer.gameObject);
                _hudContainer = null;
            }

            if (!ReferenceEquals(_speechBubble, null))
            {
                _speechBubble.StopAllCoroutines();
                _speechBubble.gameObject.SetActive(false);
                Destroy(_speechBubble.gameObject, _speechBubble.destroyTime);
                _speechBubble = null;
            }
        }

        public virtual void UpdateStatusUI()
        {
            if (!BattleRenderer.Instance.IsOnBattle)
                return;

            UpdateHpBar();
            _hudContainer.UpdatePosition(Game.instance.RaidStage.Camera.Cam, gameObject, HUDOffset);
        }

        public void AddNextBuff(Model.Buff.Buff buff)
        {
            _characterModel.AddBuff(buff);
        }

        public IEnumerator CoNormalAttack(IReadOnlyList<Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null || skillInfos.Count == 0)
            {
                yield break;
            }

            yield return StartCoroutine(CoAnimationAttack(skillInfos.Any(x => x.Critical)));
            ApplyDamage(skillInfos);
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

                var effect = Game.instance.RaidStage.SkillController.Get<SkillBlowVFX>(target, info);
                if (effect is null)
                    continue;

                effect.Play();
                ProcessAttack(target, info, true);
            }
        }

        public IEnumerator CoShatterStrike(IReadOnlyList<Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null || skillInfos.Count == 0)
                yield break;

            Vector3 effectPos = transform.position + BuffHelper.GetDefaultBuffPosition();
            var effectObj = Game.instance.Stage.objectPool.Get("ShatterStrike_casting", false, effectPos) ??
                            Game.instance.Stage.objectPool.Get("ShatterStrike_casting", true, effectPos);
            var castEffect = effectObj.GetComponent<VFX.VFX>();
            if (castEffect != null)
            {
                castEffect.Play();
            }

            Animator.Cast();
            yield return new WaitForSeconds(Game.DefaultSkillDelay);

            yield return StartCoroutine(
                    CoAnimationCastAttack(skillInfos.Any(skillInfo => skillInfo.Critical)));

            for (var i = 0; i < skillInfos.Count; i++)
            {
                var info = skillInfos[i];
                var target = info.Target.Id == Id ? this : _target;
                if (target is null)
                    continue;

                Vector3 targetEffectPos = target.transform.position;
                targetEffectPos.y = Stage.StageStartPosition + 0.32f;
                var targetEffectObj = Game.instance.Stage.objectPool.Get("ShatterStrike_magical", false, targetEffectPos) ??
                                Game.instance.Stage.objectPool.Get("ShatterStrike_magical", true, targetEffectPos);
                var strikeEffect = targetEffectObj.GetComponent<VFX.VFX>();
                if (strikeEffect is null)
                    continue;
                strikeEffect.Play();

                ProcessAttack(target, info, true);
            }
        }
        
        public IEnumerator CoDoubleAttackWithCombo(
            IReadOnlyList<Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosFirst = skillInfos.First();
            var skillInfosCount = skillInfos.Count;

            var battleWidget = Widget.Find<Nekoyume.UI.Battle>();

            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                var target = info.Target.Id == Id ? this : _target;
                if (target is null)
                    continue;

                Vector3 effectPos = target.transform.position;
                effectPos.x += 0.3f;
                effectPos.y = Stage.StageStartPosition + 0.32f;

                var first = skillInfosFirst == info;

                yield return StartCoroutine(CoAnimationAttack(info.Critical));

                var effectObj = Game.instance.Stage.objectPool.Get($"TwinAttack_0{i + 1}", false, effectPos) ??
                            Game.instance.Stage.objectPool.Get($"TwinAttack_0{i + 1}", true, effectPos);
                var effect = effectObj.GetComponent<VFX.VFX>();
                if (effect != null)
                {
                    effect.Play();
                }

                ProcessAttack(target, info, true);
                if (this is Player && !(this is EnemyPlayer))
                    battleWidget.ShowComboText(info.Effect > 0);
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

        private bool CheckAttackEnd()
        {
            return AttackEndCalled || Animator.IsIdle();
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

            CastingOnceAsync().Forget();
            foreach (var skillInfo in skillInfos)
            {
                if (skillInfo.Buff == null)
                    continue;

                yield return StartCoroutine(CoAnimationBuffCast(skillInfo));
            }

            HashSet<RaidCharacter> dispeledTargets = new HashSet<RaidCharacter>();
            foreach (var info in skillInfos)
            {
                var target = info.Target.Id == Id ? this : _target;
                target.ProcessBuff(target, info);
                if (!info.Affected || (info.DispelList != null && info.DispelList.Count() > 0))
                {
                    dispeledTargets.Add(target);
                }
            }

            Animator.Idle();

            if (dispeledTargets.Count > 0)
            {
                yield return new WaitForSeconds(.4f);
            }
            foreach (var item in dispeledTargets)
            {
                Vector3 effectPos = item.transform.position;

                var effectObj = Game.instance.Stage.objectPool.Get("buff_dispel_success", false, effectPos) ??
                            Game.instance.Stage.objectPool.Get("buff_dispel_success", true, effectPos);
                var dispellEffect = effectObj.GetComponent<VFX.VFX>();
                if (dispellEffect != null)
                {
                    dispellEffect.Play();
                }
            }
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
                    NcDebug.LogWarning($"[{nameof(RaidCharacter)}] Heal target is different from expected.");
                }

                ProcessHeal(info);
            }

            Animator.Idle();
        }

        public IEnumerator CoHealWithoutAnimation(IReadOnlyList<Skill.SkillInfo> skillInfos)
        {
            if (skillInfos is null || skillInfos.Count == 0)
                yield break;

            foreach (var info in skillInfos)
            {
                if (info.Target?.Id != Id)
                {
                    NcDebug.LogWarning($"[{nameof(RaidCharacter)}] Heal target is different from expected.");
                }

                ProcessHeal(info);
            }

            Animator.Idle();
        }

        protected virtual void ShowCutscene()
        {
        }

        protected IEnumerator CoAnimationAttack(bool isCritical)
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

                yield return new WaitForEndOfFrame();
                yield return new WaitUntil(CheckAttackEnd);
                if (Animator.IsIdle())
                {
                    continue;
                }

                break;
            }
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
            var target = info.Target;
            var copy = new Skill.SkillInfo(target.Id, target.IsDead, target.Thorn, info.Effect,
                info.Critical, info.SkillCategory,
                info.WaveTurn, ElementalType.Normal, info.SkillTargetType, info.Buff, target);
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
            yield return new WaitForSeconds(Game.DefaultSkillDelay);
        }

        private IEnumerator CoAnimationBuffCast(Skill.SkillInfo info)
        {
            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.RaidStage.BuffController.Get(pos, info.Buff);
            if (BuffCastCoroutine.TryGetValue(info.Buff.BuffInfo.Id, out var coroutine))
            {
                yield return coroutine.Invoke(effect);
            }
            else
            {
                effect.Play();
                yield return new WaitForSeconds(Game.DefaultSkillDelay);
            }
        }

        public void ShowSpeech(string key, params int[] list)
        {
            if (!_speechBubble)
            {
                return;
            }

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
                Game.instance.RaidStage.Camera.Shake();
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
                DamageText.Show(Game.instance.RaidStage.Camera.Cam, position, force, dmg, group);
                if (info.SkillCategory == Nekoyume.Model.Skill.SkillCategory.NormalAttack)
                    VFXController.instance.Create<BattleAttack01VFX>(pos);
            }
        }

        protected void ApplyDamage(IReadOnlyList<Skill.SkillInfo> skillInfos)
        {
            for (var i = 0; i < skillInfos.Count; i++)
            {
                var info = skillInfos[i];
                var target = info.Target.Id == Id ? this : _target;
                ProcessAttack(target, info, false);
                if (info.Target is Model.Enemy)
                {
                    _worldBossBattle.ShowComboText(info.Effect > 0);
                }
            }
        }

        private RaidCharacter GetTarget(Skill.SkillInfo info)
        {
            return info.Target.Id == Id ? this : _target;
        }

        protected void ProcessAttack(RaidCharacter target, Skill.SkillInfo skill, bool isConsiderElementalType)
        {
            ShowSpeech("PLAYER_SKILL", (int)skill.ElementalType, (int)skill.SkillCategory);
            target.ProcessDamage(skill, isConsiderElementalType);
            ShowSpeech("PLAYER_ATTACK");
        }

        protected void ProcessBuff(RaidCharacter target, Skill.SkillInfo info)
        {
            if (IsDead)
            {
                return;
            }

            var buff = info.Buff;
            var effect = Game.instance.RaidStage.BuffController.Get<BuffVFX>(target.gameObject, buff);
            effect.Target = target;
            effect.Buff = buff;

            effect.Play();
            if (effect.IsPersisting)
            {
                target.AttachPersistingVFX(buff.BuffInfo.GroupId, effect);
                StartCoroutine(BuffController.CoChaseTarget(effect, target, buff));
            }

            target.AddNextBuff(buff);
            target.UpdateStatusUI();
        }

        private void AttachPersistingVFX(int groupId, BuffVFX vfx)
        {
            if (_persistingVFXMap.TryGetValue(groupId, out var prevVFX))
            {
                prevVFX.Stop();
                _persistingVFXMap.Remove(groupId);
            }

            _persistingVFXMap[groupId] = vfx;
        }

        protected void ProcessHeal(Skill.SkillInfo info)
        {
            if (IsDead)
            {
                return;
            }

            _currentHp = Math.Min(_currentHp + info.Effect, _characterModel.HP);

            var position = transform.TransformPoint(0f, 1.7f, 0f);
            var force = new Vector3(-0.1f, 0.5f);
            var txt = info.Effect.ToString();
            DamageText.Show(Game.instance.RaidStage.Camera.Cam, position, force, txt, DamageText.TextGroupState.Heal);
            VFXController.instance.CreateAndChase<BattleHeal01VFX>(transform, HealOffset);
        }

        public virtual void ProcessDamage(Skill.SkillInfo info, bool isConsiderElementalType)
        {
            var dmg = info.Effect;

            var position = transform.TransformPoint(0f, 1.7f, 0f);
            var force = DamageTextForce;

            if (dmg <= 0)
            {
                var index = _characterModel is Model.Player ? 1 : 0;
                MissText.Show(Game.instance.RaidStage.Camera.Cam, position, force, index);
                return;
            }

            var value = _currentHp - dmg;
            _currentHp = Math.Clamp(value, 0, _characterModel.HP);

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
            foreach (var vfx in _persistingVFXMap.Values)
            {
                vfx.Stop();
            }
        }

        protected virtual void OnDeadEnd()
        {
            Animator.Idle();
        }
    }
}
