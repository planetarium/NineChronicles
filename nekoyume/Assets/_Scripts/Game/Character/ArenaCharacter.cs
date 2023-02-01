using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BTAI;
using DG.Tweening;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.UI;
using UnityEngine;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;

namespace Nekoyume.Game.Character
{
    using Nekoyume.Model;
    using UniRx;

    public class ArenaCharacter : Character
    {
        public Model.ArenaCharacter CharacterModel { get; set; }

        [SerializeField]
        private CharacterAppearance appearance;

        private static Vector3 DamageTextForce => new Vector3(-0.1f, 0.5f);
        private const float AnimatorTimeScale = 1.2f;
        private const float StartPos = 2.5f;
        private const float RunDuration = 1f;
        private float AttackTime;
        private float CriticalAttackTime;

        private ArenaCharacter _target;
        private ArenaBattle _arenaBattle;
        private ArenaActionParams _runningAction;
        private HudContainer _hudContainer;
        private SpeechBubble _speechBubble;
        private Root _root;
        private bool _forceStop;
        private int _currentHp;
        private readonly List<Costume> _costumes = new List<Costume>();
        private readonly List<Equipment> _equipments = new List<Equipment>();
        private readonly Dictionary<int, VFX.VFX> _persistingVFXMap = new();

        public List<ArenaActionParams> Actions { get; } = new List<ArenaActionParams>();

        private bool IsDead => _currentHp <= 0;

        private void Awake()
        {
            Animator = new PlayerAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = AnimatorTimeScale;
        }

        private void Update()
        {
            _root?.Tick();
        }

        private void LateUpdate()
        {
            _hudContainer.UpdatePosition(ActionCamera.instance.Cam, gameObject, HUDOffset);
            _speechBubble.UpdatePosition(ActionCamera.instance.Cam, gameObject, HUDOffset);
        }

        private void OnDisable()
        {
            foreach (var vfx in _persistingVFXMap.Values)
            {
                vfx.gameObject.SetActive(false);
            }
            _persistingVFXMap.Clear();
        }

        public void Init(ArenaPlayerDigest digest, ArenaCharacter target, bool isEnemy)
        {
            gameObject.SetActive(true);
            transform.localPosition = new Vector3(isEnemy ? StartPos : -StartPos, -1.2f, 0);

            _hudContainer ??= Widget.Create<HudContainer>(true);
            _speechBubble ??= Widget.Create<SpeechBubble>();
            _arenaBattle ??= Widget.Find<ArenaBattle>();

            Animator.Idle();
            _costumes.Clear();
            _costumes.AddRange(digest.Costumes);
            _equipments.Clear();
            _equipments.AddRange(digest.Equipments);
            _target = target;
            appearance.Set(digest, Animator, _hudContainer);
            AttackTime = SpineAnimationHelper.GetAnimationDuration(appearance, "Attack");
            CriticalAttackTime = SpineAnimationHelper.GetAnimationDuration(appearance, "CriticalAttack");
        }

        public void Spawn(Model.ArenaCharacter model)
        {
            CharacterModel = model;
            Id = CharacterModel.Id;
            SizeType = CharacterModel.SizeType;
            _currentHp = CharacterModel.HP;
            UpdateStatusUI();
            _arenaBattle.ShowStatus(CharacterModel.IsEnemy);
            Animator.Run();
            var endPos = appearance.BoxCollider.size.x * 0.5f;
            transform.DOLocalMoveX(CharacterModel.IsEnemy ? endPos : -endPos, RunDuration)
                .SetEase(Ease.OutSine)
                .OnComplete(() =>
                {
                    Animator.StopRun();
                    InitBT();
                });
        }

        public void UpdateStatusUI()
        {
            if (!Game.instance.IsInWorld)
                return;

            _hudContainer.UpdatePosition(ActionCamera.instance.Cam, gameObject, HUDOffset);
            _arenaBattle.UpdateStatus(CharacterModel.IsEnemy, _currentHp, CharacterModel.HP, CharacterModel.Buffs);


            // delete existing vfx
            var removedVfx = new List<int>();
            foreach (var buff in _persistingVFXMap.Keys)
            {
                if (CharacterModel.IsDead ||
                    !CharacterModel.Buffs.Keys.Contains(buff))
                {
                    _persistingVFXMap[buff].LazyStop();
                    removedVfx.Add(buff);
                }
            }

            foreach (var id in removedVfx)
            {
                _persistingVFXMap.Remove(id);
            }
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
            ArenaSkill.ArenaSkillInfo info,
            bool isConsiderElementalType)
        {
            var dmg = info.Effect.ToString();
            var pos = transform.position;
            pos.x -= 0.2f;
            pos.y += 0.32f;
            var group = CharacterModel.IsEnemy
                ? DamageText.TextGroupState.Damage
                : DamageText.TextGroupState.Basic;

            if (info.Critical)
            {
                ActionCamera.instance.Shake();
                AudioController.PlayDamagedCritical();
                CriticalText.Show(position, force, dmg, group);
                if (info.SkillCategory == SkillCategory.NormalAttack)
                    VFXController.instance.Create<BattleAttackCritical01VFX>(pos);
            }
            else
            {
                AudioController.PlayDamaged(isConsiderElementalType
                    ? info.ElementalType
                    : ElementalType.Normal);
                DamageText.Show(ActionCamera.instance.Cam, position, force, dmg, group);
                if (info.SkillCategory == SkillCategory.NormalAttack)
                    VFXController.instance.Create<BattleAttack01VFX>(pos);
            }
        }

        private void InitBT()
        {
            if (_root != null)
            {
                return;
            }

            _root = new Root();
            _root.OpenBranch(
                BT.Selector().OpenBranch(
                    BT.If(() => Actions.Any()).OpenBranch(
                        BT.Call(ExecuteAction)
                    )
                )
            );
        }

        private void ExecuteAction()
        {
            StartCoroutine(CoExecuteAction());
        }

        private IEnumerator CoExecuteAction()
        {
            if (_runningAction is null)
            {
                _runningAction = Actions.First();

                var arena = Game.instance.Arena;
                var waitSeconds = StageConfig.instance.actionDelay;

                foreach (var info in _runningAction.skillInfos)
                {
                    if (info.Target is Model.ArenaCharacter arenaCharacter)
                    {
                        if (arenaCharacter.IsDead)
                        {
                            var target = info.Target.Id == Id ? this : _target;
                            if (target.Actions.Any())
                            {
                                var time = Time.time;
                                yield return new WaitWhile(() =>
                                    Time.time - time > 10f || target.Actions.Any());
                            }
                        }
                    }
                }

                yield return new WaitForSeconds(waitSeconds);
                var coroutine = StartCoroutine(arena.CoSkill(_runningAction));
                yield return coroutine;
                Actions.Remove(_runningAction);
                _runningAction = null;
            }
        }

        private void ProcessAttack(ArenaCharacter target, ArenaSkill.ArenaSkillInfo skill, bool isConsiderElementalType)
        {
            ShowSpeech("PLAYER_SKILL", (int)skill.ElementalType, (int)skill.SkillCategory);
            StartCoroutine(target.CoProcessDamage(skill, isConsiderElementalType));
            ShowSpeech("PLAYER_ATTACK");
        }

        public IEnumerator CoProcessDamage(ArenaSkill.ArenaSkillInfo info, bool isConsiderElementalType)
        {
            CharacterModel = info.Target;
            var dmg = info.Effect;

            var position = transform.TransformPoint(0f, 1.7f, 0f);
            var force = DamageTextForce;

            if (dmg <= 0)
            {
                var index = CharacterModel.IsEnemy ? 1 : 0;
                MissText.Show(ActionCamera.instance.Cam, position, force, index);
                yield break;
            }

            var value = _currentHp - dmg;
            _currentHp = Mathf.Clamp(value, 0, CharacterModel.HP);

            UpdateStatusUI();
            PopUpDmg(position, force, info, isConsiderElementalType);
        }

        private void ProcessHeal(ArenaSkill.ArenaSkillInfo info)
        {
            if (IsDead)
            {
                return;
            }

            _currentHp = Math.Min(_currentHp + info.Effect, CharacterModel.HP);

            var position = transform.TransformPoint(0f, 1.7f, 0f);
            var force = new Vector3(-0.1f, 0.5f);
            var txt = info.Effect.ToString();
            DamageText.Show(ActionCamera.instance.Cam, position, force, txt, DamageText.TextGroupState.Heal);
            VFXController.instance.CreateAndChase<BattleHeal01VFX>(transform, HealOffset);
        }

        private void ProcessBuff(ArenaCharacter target, ArenaSkill.ArenaSkillInfo info)
        {
            if (IsDead)
            {
                return;
            }

            var buff = info.Buff;
            var effect = Game.instance.Arena.BuffController.Get<ArenaCharacter, BuffVFX>(target, buff);
            effect.Play();
            if (effect.IsPersisting)
            {
                target.AttachPersistingVFX(buff.BuffInfo.GroupId, effect);
                StartCoroutine(BuffController.CoChaseTarget(effect, target.transform));
            }

            target.CharacterModel = info.Target;
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

        #region Animation

        private void ShowCutscene()
        {
            if (_costumes.Exists(x => x.ItemSubType == ItemSubType.FullCostume))
            {
                return;
            }

            var armor = _equipments.FirstOrDefault(x => x.ItemSubType == ItemSubType.Armor);
            AreaAttackCutscene.Show(armor?.Id ?? GameConfig.DefaultAvatarArmorId);
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

            yield return new WaitForSeconds(isCritical ? CriticalAttackTime : AttackTime);
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

        private IEnumerator CoAnimationCastBlow(IReadOnlyList<ArenaSkill.ArenaSkillInfo> infos)
        {
            var info = infos.First();
            var copy = new ArenaSkill.ArenaSkillInfo(info.Target, info.Effect,
                info.Critical, info.SkillCategory,
                info.Turn, ElementalType.Normal, info.SkillTargetType, info.Buff);
            yield return StartCoroutine(CoAnimationCast(copy));

            var pos = transform.position;
            yield return CoAnimationCastAttack(infos.Any(skillInfo => skillInfo.Critical));
            var effect = Game.instance.Arena.SkillController.GetBlowCasting(
                pos,
                info.SkillCategory,
                info.ElementalType);
            effect.Play();
            yield return new WaitForSeconds(0.2f);
        }

        protected virtual IEnumerator CoAnimationCast(ArenaSkill.ArenaSkillInfo info)
        {
            ShowSpeech("PLAYER_SKILL", (int)info.ElementalType, (int)info.SkillCategory);
            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.Arena.SkillController.Get(pos, info.ElementalType);
            effect.Play();
            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator CoAnimationBuffCast(ArenaSkill.ArenaSkillInfo info)
        {
            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            var effect = Game.instance.Arena.BuffController.Get(pos, info.Buff);
            effect.Play();
            yield return new WaitForSeconds(0.6f);
        }
        #endregion

        #region Skill

        public IEnumerator CoNormalAttack(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
        {
            if (skillInfos is null || skillInfos.Count == 0)
            {
                yield break;
            }

            ActionPoint = () => { ApplyDamage(skillInfos); };
            yield return StartCoroutine(CoAnimationAttack(skillInfos.Any(x => x.Critical)));
        }

        private void ApplyDamage(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
        {
            for (var i = 0; i < skillInfos.Count; i++)
            {
                var info = skillInfos[i];
                if (Game.instance.Arena.TurnNumber != info.Turn)
                {
                    continue;
                }

                if (info.Target is Model.ArenaCharacter targetArenaCharacter)
                {
                    var target = info.Target.Id == Id ? this : _target;

                    ProcessAttack(target, info, false);

                    if (targetArenaCharacter.IsEnemy)
                    {
                        _arenaBattle.ShowComboText(info.Effect > 0);
                    }
                }
            }
        }

        public IEnumerator CoBlowAttack(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosCount = skillInfos.Count;

            ActionPoint = () =>
            {
                for (var i = 0; i < skillInfosCount; i++)
                {
                    var info = skillInfos[i];
                    var target = info.Target.Id == Id ? this : _target;
                    var effect = Game.instance.Arena.SkillController.Get<SkillBlowVFX>(target, info);
                    if (effect is null)
                        continue;

                    effect.Play();
                    ProcessAttack(target, info, true);
                }
            };
            if (skillInfos.First().SkillTargetType == SkillTargetType.Enemy)
            {
                yield return StartCoroutine(CoAnimationCast(skillInfos.First()));
                yield return StartCoroutine(
                    CoAnimationCastAttack(skillInfos.Any(skillInfo => skillInfo.Critical)));
            }
            else
            {
                yield return StartCoroutine(CoAnimationCastBlow(skillInfos));
            }
        }

        public IEnumerator CoDoubleAttack(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
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
                ActionPoint = () =>
                {
                    var target = info.Target.Id == Id ? this : _target;
                    var effect =
                        Game.instance.Arena.SkillController.Get<SkillDoubleVFX>(target, info);
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
                };
                yield return StartCoroutine(CoAnimationAttack(info.Critical));
            }
        }

        public IEnumerator CoAreaAttack(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            var skillInfosFirst = skillInfos.First();
            var skillInfosCount = skillInfos.Count;

            ShowCutscene();
            yield return StartCoroutine(CoAnimationCast(skillInfosFirst));

            var effectTarget = skillInfosFirst.Target.Id == Id ? this : _target;
            var effect = Game.instance.Arena.SkillController.Get<SkillAreaVFX>(effectTarget,
                    skillInfosFirst);
            if (effect is null)
                yield break;

            ArenaSkill.ArenaSkillInfo trigger = null;
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

                    ActionPoint = () =>
                    {
                        ProcessAttack(target, info,  true);
                    };
                    var coroutine = StartCoroutine(CoAnimationCastAttack(info.Critical));
                    if (info.ElementalType == ElementalType.Water)
                    {
                        yield return new WaitForSeconds(0.1f);
                        effect.StopLoop();
                    }

                    yield return coroutine;
                    effect.Finisher();
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

        public IEnumerator CoHeal(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
        {
            if (skillInfos is null || skillInfos.Count == 0)
                yield break;

            yield return StartCoroutine(CoAnimationCast(skillInfos.First()));

            foreach (var info in skillInfos)
            {
                var target = info.Target.Id == Id ? this : _target;
                target.ProcessHeal(info);
            }

            Animator.Idle();
        }

        public IEnumerator CoBuff(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
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

        public IEnumerator CoTickDamage(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
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

        #endregion

        public void Dead()
        {
            StartCoroutine(Dying());
        }

        private IEnumerator Dying()
        {
            yield return new WaitWhile(() => Actions.Any());
            OnDeadStart();
            yield return new WaitForSeconds(0.2f);
            _speechBubble.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.8f);
            OnDeadEnd();
        }

        private void OnDeadStart()
        {
            ShowSpeech("PLAYER_LOSE");
            Animator.Die();
            foreach (var vfx in _persistingVFXMap.Values)
            {
                vfx.LazyStop();
            }
        }

        private void OnDeadEnd()
        {
            Animator.Idle();
            Actions.Clear();
            gameObject.SetActive(false);
            _root = null;
            _runningAction = null;
        }
    }
}
