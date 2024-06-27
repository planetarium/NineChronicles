using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BTAI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Libplanet.Crypto;
using Nekoyume.Game.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using Nekoyume.Helper;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.UI;
using UnityEngine;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;

namespace Nekoyume.Game.Character
{
    using Model;
    using UniRx;

    public class ArenaCharacter : Character
    {
        public Model.ArenaCharacter CharacterModel { get; set; }

        [SerializeField]
        private CharacterAppearance appearance;

        private static Vector3 DamageTextForce => new Vector3(-0.1f, 0.5f);
        private const float AnimatorTimeScale = 2.5f;
        private const float StartPos = 2.5f;
        private const float RunDuration = 1f;

        private ArenaCharacter _target;
        private ArenaBattle _arenaBattle;
        private ArenaActionParams _runningAction;
        private HudContainer _hudContainer;
        private SpeechBubble _speechBubble;
        private Root _root;
        // TODO: 어디에 쓰는것인지 모르겠음
        private bool _forceStop;
        private long _currentHp;
        private readonly List<Costume> _costumes = new List<Costume>();
        private readonly List<Equipment> _equipments = new List<Equipment>();
        private readonly Dictionary<int, VFX.VFX> _persistingVFXMap = new();

        private readonly Queue<ArenaActionParams> _actionQueue = new();
        
        public Pet Pet => appearance.Pet;

        private bool IsDead => _currentHp <= 0;

        private readonly List<int> removedBuffVfxList = new();

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
            _actionQueue.Clear();
        }

        public void Init(
            ArenaPlayerDigest digest,
            Address avatarAddress,
            ArenaCharacter target,
            bool isEnemy)
        {
            IsFlipped = isEnemy;
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
            appearance.Set(digest, avatarAddress, Animator, _hudContainer);
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
            if (!BattleRenderer.Instance.IsOnBattle)
                return;

            _hudContainer.UpdatePosition(ActionCamera.instance.Cam, gameObject, HUDOffset);
            _arenaBattle.UpdateStatus(CharacterModel.IsEnemy, _currentHp, CharacterModel.HP, CharacterModel.Buffs);
            UpdateBuffVfx();
        }

        public virtual void UpdateBuffVfx()
        {
            // delete existing vfx
            removedBuffVfxList.Clear();
            foreach (var buff in _persistingVFXMap.Keys)
            {
                if (!CharacterModel.IsDead && CharacterModel.Buffs.Keys.Contains(buff))
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
                    BT.If(() => _actionQueue.Any()).OpenBranch(
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
            if (_runningAction is not null)
            {
                yield break;
            }
            _runningAction = _actionQueue.Dequeue();

            var cts = new CancellationTokenSource();
            ActionTimer(cts).Forget();

            foreach (var info in _runningAction.skillInfos)
            {
                var target = info.Target;
                if (!target.IsDead)
                {
                    continue;
                }
                
                var targetActor = info.Target.Id == Id ? this : _target;
                if (!targetActor || targetActor == this || !targetActor.HasAction())
                {
                    continue;
                }

                var time = Time.time;
                yield return new WaitUntil(() => Time.time - time > 10f || !targetActor.HasAction());
                if (Time.time - time > 10f)
                {
                    NcDebug.LogError($"[{nameof(ArenaCharacter)}] CoExecuteAction Timeout. {gameObject.name}");
                    break;
                }
            }

            yield return new WaitForSeconds(StageConfig.instance.actionDelay);
            if (_runningAction != null)
            {
                yield return StartCoroutine(Game.instance.Arena.CoSkill(_runningAction));
            }
            _runningAction = null;
            // TODO: ForceStop??
            cts.Cancel();
            cts.Dispose();
        }

        private async UniTask ActionTimer(CancellationTokenSource cts)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(15), cancellationToken: cts.Token);
            NcDebug.LogWarning($"[{nameof(ArenaCharacter)}] ActionTimer Timeout. {gameObject.name}");
            _runningAction = null;
        }

        public bool HasAction()
        {
            return _actionQueue.Any() || _runningAction is not null;
        }

        public void AddAction(ArenaActionParams actionParams)
        {
            _actionQueue.Enqueue(actionParams);
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
            _currentHp = Math.Clamp(value, 0, CharacterModel.HP);

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
            var effect = Game.instance.Arena.BuffController.Get<BuffVFX>(target.gameObject, buff);
            effect.Target = target;
            effect.Buff = buff;

            effect.Play();
            if (effect.IsPersisting)
            {
                target.AttachPersistingVFX(buff.BuffInfo.GroupId, effect);
                StartCoroutine(BuffController.CoChaseTarget(effect, target, buff));
            }

            OnBuff?.Invoke(buff.BuffInfo.GroupId);

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

            yield return new WaitForSeconds(Animator.AnimationLength());
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
            yield return new WaitForSeconds(Game.DefaultSkillDelay);
        }

        private IEnumerator CoAnimationBuffCast(ArenaSkill.ArenaSkillInfo info)
        {
            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            var pos = transform.position;
            var effect = Game.instance.Arena.BuffController.Get(pos, info.Buff);

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

        public IEnumerator CoShatterStrike(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
                yield break;

            ActionPoint = () =>
            {
                for (var i = 0; i < skillInfos.Count; i++)
                {
                    var info = skillInfos[i];
                    var target = info.Target.Id == Id ? this : _target;
                    if (target is null)
                        continue;

                    Vector3 targetEffectPos = target.transform.position + BuffHelper.GetDefaultBuffPosition();
                    var targetEffectObj = Game.instance.Stage.objectPool.Get("ShatterStrike_magical", false, targetEffectPos) ??
                                    Game.instance.Stage.objectPool.Get("ShatterStrike_magical", true, targetEffectPos);
                    var strikeEffect = targetEffectObj.GetComponent<VFX.VFX>();
                    if (strikeEffect is null)
                        continue;
                    strikeEffect.Play();

                    ProcessAttack(target, info, false);
                }
            };

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
        }

        public IEnumerator CoDoubleAttackWithCombo(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
        {
            if (skillInfos is null || skillInfos.Count == 0)
            {
                yield break;
            }

            var skillInfosCount = skillInfos.Count;
            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                ActionPoint = () =>
                {
                    var target = info.Target.Id == Id ? this : _target;

                    Vector3 effectPos = target.transform.position;
                    effectPos.x += 0.3f;
                    effectPos.y = Stage.StageStartPosition + 0.32f;

                    var effectObj = Game.instance.Stage.objectPool.Get($"TwinAttack_{i + 1:D2}", false, effectPos) ??
                    Game.instance.Stage.objectPool.Get($"TwinAttack_0{i + 1}", true, effectPos);
                    var effect = effectObj.GetComponent<VFX.VFX>();
                    if (effect != null)
                    {
                        effect.Play();
                    }

                    ProcessAttack(target, info, true);
                };
                yield return StartCoroutine(CoAnimationAttack(info.Critical));
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

        public IEnumerator CoHealWithoutAnimation(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
        {
            if (skillInfos is null || skillInfos.Count == 0)
                yield break;

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

            CastingOnceAsync().Forget();
            foreach (var skillInfo in skillInfos)
            {
                if (skillInfo.Buff == null)
                    continue;

                yield return StartCoroutine(CoAnimationBuffCast(skillInfo));
            }

            HashSet<ArenaCharacter> dispeledTargets = new HashSet<ArenaCharacter>();
            foreach (var info in skillInfos)
            {
                var target = info.Target.Id == Id ? this : _target;
                target.ProcessBuff(target, info);
                if (!info.Affected || (info.DispelList != null && info.DispelList.Count() > 0))
                {
                    dispeledTargets.Add(target);
                }
            }

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

        public IEnumerator CoTickDamage(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
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

            foreach (var info in skillInfos)
            {
                var target = info.Target.Id == Id ? this : _target;
                target.ProcessBuff(target, info);
            }
        }

        #endregion

        public void Dead()
        {
            StartCoroutine(Dying());
        }

        private IEnumerator Dying()
        {
            yield return new WaitWhile(HasAction);
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
            _actionQueue.Clear();
            gameObject.SetActive(false);
            _root = null;
            _runningAction = null;
            _actionQueue.Clear();
        }
    }
}
