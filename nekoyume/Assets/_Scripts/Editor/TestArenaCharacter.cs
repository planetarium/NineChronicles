using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BTAI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SimulationTest;
using Libplanet.Crypto;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.VFX;
using Nekoyume.Model.BattleStatus.Arena;
using Nekoyume.UI;
using UnityEngine;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;
using UnityEngine.Serialization;

namespace Nekoyume.Editor
{
    using Model;
    using UniRx;

    public class TestArenaCharacter : Character
    {
        public ArenaCharacter CharacterModel { get; set; }

        [SerializeField]
        private CharacterAppearance appearance;

        [SerializeField]
        private ArenaBattle arenaBattle;

        private static Vector3 DamageTextForce => new(-0.1f, 0.5f);
        private const float AnimatorTimeScale = 2.5f;
        private const float StartPos = 2.5f;
        private const float RunDuration = 1f;

        private TestArenaCharacter _target;
        private ArenaActionParams _runningAction;
        private HudContainer _hudContainer;
        private SpeechBubble _speechBubble;

        private Root _root;

        // TODO: 어디에 쓰는것인지 모르겠음
        private bool _forceStop;
        private long _currentHp;
        private readonly List<Costume> _costumes = new();
        private readonly List<Equipment> _equipments = new();

        private readonly Queue<ArenaActionParams> _actionQueue = new();

        private bool IsDead => _currentHp <= 0;

        private readonly List<int> removedBuffVfxList = new();

        private void Awake()
        {
            Animator = new PlayerAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = AnimatorTimeScale;
        }

        protected override void Update()
        {
            base.Update();
            _root?.Tick();
        }

        private void LateUpdate()
        {
            _hudContainer.UpdatePosition(ActionCamera.instance.Cam, gameObject, HUDOffset);
            _speechBubble.UpdatePosition(ActionCamera.instance.Cam, gameObject, HUDOffset);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _actionQueue.Clear();
        }

        public void Init(
            ArenaPlayerDigest digest,
            Address avatarAddress,
            TestArenaCharacter target,
            bool isEnemy)
        {
            IsFlipped = isEnemy;
            gameObject.SetActive(true);
            transform.localPosition = new Vector3(isEnemy ? StartPos : -StartPos, -1.2f, 0);

            _hudContainer ??= Widget.Create<HudContainer>(true);
            _speechBubble ??= Widget.Create<SpeechBubble>();

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
            arenaBattle.ShowStatus(CharacterModel.IsEnemy);
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
            _hudContainer.UpdatePosition(ActionCamera.instance.Cam, gameObject, HUDOffset);
            arenaBattle.UpdateStatus(CharacterModel.IsEnemy, _currentHp, CharacterModel.HP, CharacterModel.Buffs);
            UpdateBuffVfx();
        }

        public virtual void UpdateBuffVfx()
        {
            // delete existing vfx
            removedBuffVfxList.Clear();
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
            {
                return;
            }

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
            }
            else
            {
                AudioController.PlayDamaged(isConsiderElementalType
                    ? info.ElementalType
                    : ElementalType.Normal);
                DamageText.Show(ActionCamera.instance.Cam, position, force, dmg, group);
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
                yield return StartCoroutine(TestArena.Instance.CoSkill(_runningAction));
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

        private void ProcessAttack(TestArenaCharacter target, ArenaSkill.ArenaSkillInfo skill, bool isConsiderElementalType)
        {
            ShowSpeech("PLAYER_SKILL", (int)skill.ElementalType, (int)skill.SkillCategory);
            NcDebug.Log(
                $"[ProcessAttack] Caster: {(TestArena.Instance.Me.Id == Id ? "me" : "enemy")}, Target: {(TestArena.Instance.Me.Id == target.Id ? "me" : "enemy")}, ElementalType: {skill.ElementalType}, SkillCategory: {skill.SkillCategory}",
                "BattleSimulation");
            StartCoroutine(target.CoProcessDamage(skill, isConsiderElementalType, Id));
            ShowSpeech("PLAYER_ATTACK");
        }

        public IEnumerator CoProcessDamage(ArenaSkill.ArenaSkillInfo info, bool isConsiderElementalType, Guid casterId)
        {
            CharacterModel = info.Target;
            var dmg = info.Effect;

            var position = transform.TransformPoint(0f, 1.7f, 0f);
            var force = DamageTextForce;
            NcDebug.Log($"[CoProcessDamage] Caster: {(TestArena.Instance.Me.Id == casterId ? "me" : "enemy")}, Target: {(TestArena.Instance.Me.Id == CharacterModel.Id ? "me" : "enemy")}, dmg: {dmg}, pre-HP: {CharacterModel.CurrentHP}", "BattleSimulation");
            if (dmg <= 0)
            {
                var index = CharacterModel.IsEnemy ? 1 : 0;
                MissText.Show(ActionCamera.instance.Cam, position, force, index);
                NcDebug.Log($"[CoProcessDamage] Caster: {(TestArena.Instance.Me.Id == casterId ? "me" : "enemy")}, Target: {(TestArena.Instance.Me.Id == CharacterModel.Id ? "me" : "enemy")}, MISS", "BattleSimulation");
                yield break;
            }

            var value = _currentHp - dmg;
            _currentHp = Math.Clamp(value, 0, CharacterModel.HP);

            UpdateStatusUI();
            PopUpDmg(position, force, info, isConsiderElementalType);
            NcDebug.Log($"[CoProcessDamage] Caster: {(TestArena.Instance.Me.Id == casterId ? "me" : "enemy")}, Target: {(TestArena.Instance.Me.Id == CharacterModel.Id ? "me" : "enemy")}, dmg: {dmg}, processed-HP: {_currentHp}", "BattleSimulation");
        }

        private void ProcessHeal(ArenaSkill.ArenaSkillInfo info)
        {
            if (IsDead)
            {
                NcDebug.Log($"[ProcessHeal] Caster: {(TestArena.Instance.Me.Id == Id ? "me" : "enemy")}, Target: {(TestArena.Instance.Me.Id == CharacterModel.Id ? "me" : "enemy")}, effect: {info.Effect}, IsDead", "BattleSimulation");
                return;
            }

            NcDebug.Log($"[ProcessHeal] Caster: {(TestArena.Instance.Me.Id == Id ? "me" : "enemy")}, Target: {(TestArena.Instance.Me.Id == CharacterModel.Id ? "me" : "enemy")}, effect: {info.Effect}, pre-HP: {CharacterModel.CurrentHP}", "BattleSimulation");
            _currentHp = Math.Min(_currentHp + info.Effect, CharacterModel.HP);

            var position = transform.TransformPoint(0f, 1.7f, 0f);
            var force = new Vector3(-0.1f, 0.5f);
            var txt = info.Effect.ToString();
            DamageText.Show(ActionCamera.instance.Cam, position, force, txt, DamageText.TextGroupState.Heal);
            NcDebug.Log($"[ProcessHeal] Caster: {(TestArena.Instance.Me.Id == Id ? "me" : "enemy")}, Target: {(TestArena.Instance.Me.Id == CharacterModel.Id ? "me" : "enemy")}, effect: {info.Effect}, processed-HP: {_currentHp}", "BattleSimulation");
        }

        private void ProcessBuff(TestArenaCharacter target, ArenaSkill.ArenaSkillInfo info)
        {
            if (IsDead)
            {
                NcDebug.Log($"[ProcessBuff] Caster: {(TestArena.Instance.Me.Id == Id ? "me" : "enemy")}, Target: {(TestArena.Instance.Me.Id == info.Target.Id ? "me" : "enemy")}, buff: {info.Buff}, IsDead", "BattleSimulation");
                return;
            }

            NcDebug.Log(
                $"[ProcessBuff] Caster: {(TestArena.Instance.Me.Id == Id ? "me" : "enemy")}, Target: {(TestArena.Instance.Me.Id == info.Target.Id ? "me" : "enemy")}, buff: {info.Buff}, skillTarget: {info.Buff.BuffInfo.SkillTargetType}, buffGroupId: {info.Buff.BuffInfo.GroupId}, buffId: {info.Buff.BuffInfo.Id}",
                "BattleSimulation");
            var buff = info.Buff;
            OnBuff?.Invoke(buff.BuffInfo.GroupId);

            target.CharacterModel = info.Target;
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
            yield return new WaitForSeconds(0.2f);
        }

        protected virtual IEnumerator CoAnimationCast(ArenaSkill.ArenaSkillInfo info)
        {
            ShowSpeech("PLAYER_SKILL", (int)info.ElementalType, (int)info.SkillCategory);
            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            Animator.Cast();
            var pos = transform.position;
            yield return new WaitForSeconds(.6f);
        }

        private IEnumerator CoAnimationBuffCast(ArenaSkill.ArenaSkillInfo info)
        {
            var sfxCode = AudioController.GetElementalCastingSFX(info.ElementalType);
            AudioController.instance.PlaySfx(sfxCode);
            var pos = transform.position;

            yield return new WaitForSeconds(.6f);
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
                if (TestArena.Instance.TurnNumber != info.Turn)
                {
                    continue;
                }

                if (info.Target is Model.ArenaCharacter targetArenaCharacter)
                {
                    var target = info.Target.Id == Id ? this : _target;

                    ProcessAttack(target, info, false);

                    if (targetArenaCharacter.IsEnemy)
                    {
                        arenaBattle.ShowComboText(info.Effect > 0);
                    }
                }
            }
        }

        public IEnumerator CoBlowAttack(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
            {
                yield break;
            }

            var skillInfosCount = skillInfos.Count;

            ActionPoint = () =>
            {
                for (var i = 0; i < skillInfosCount; i++)
                {
                    var info = skillInfos[i];
                    var target = info.Target.Id == Id ? this : _target;
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
            {
                yield break;
            }

            ActionPoint = () =>
            {
                for (var i = 0; i < skillInfos.Count; i++)
                {
                    var info = skillInfos[i];
                    var target = info.Target.Id == Id ? this : _target;
                    if (target is null)
                    {
                        continue;
                    }

                    ProcessAttack(target, info, false);
                }
            };

            Animator.Cast();
            yield return new WaitForSeconds(.6f);

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

                    ProcessAttack(target, info, true);
                };
                yield return StartCoroutine(CoAnimationAttack(info.Critical));
            }
        }

        public IEnumerator CoAreaAttack(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
            {
                yield break;
            }

            var skillInfosFirst = skillInfos.First();
            var skillInfosCount = skillInfos.Count;

            ShowCutscene();
            yield return StartCoroutine(CoAnimationCast(skillInfosFirst));

            yield return new WaitForSeconds(0.5f);
            for (var i = 0; i < skillInfosCount; i++)
            {
                var info = skillInfos[i];
                var target = info.Target.Id == Id ? this : _target;

                yield return new WaitForSeconds(0.14f);
                ProcessAttack(target, info, false);
            }

            yield return new WaitForSeconds(0.5f);
        }

        public IEnumerator CoHeal(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
        {
            if (skillInfos is null || skillInfos.Count == 0)
            {
                yield break;
            }

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
            {
                yield break;
            }

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
            {
                yield break;
            }

            CastingOnceAsync().Forget();
            foreach (var skillInfo in skillInfos)
            {
                if (skillInfo.Buff == null)
                {
                    continue;
                }

                yield return StartCoroutine(CoAnimationBuffCast(skillInfo));
            }

            var dispeledTargets = new HashSet<TestArenaCharacter>();
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
        }

        public IEnumerator CoTickDamage(IReadOnlyList<ArenaSkill.ArenaSkillInfo> skillInfos)
        {
            if (skillInfos is null ||
                skillInfos.Count == 0)
            {
                yield break;
            }

            CastingOnceAsync().Forget();
            foreach (var skillInfo in skillInfos)
            {
                if (skillInfo.Buff == null)
                {
                    continue;
                }

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

        public override void SetSpineColor(Color color, int propertyID = -1)
        {
            base.SetSpineColor(color, propertyID);
            if (appearance != null)
            {
                appearance.SetSpineColor(color, propertyID);
            }
        }
    }
}
