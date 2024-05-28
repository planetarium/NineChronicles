using Nekoyume.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    using UniRx;

    public class StageMonster : Actor
    {
        private Player _player;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        // todo: 적의 이동속도에 따라서 인게임 연출 버그가 발생할 수 있으니 '-1f'로 값을 고정함. 이후 이 문제를 해결해서 몬스터 별 이동속도를 구현할 필요가 있음.
        protected override float RunSpeedDefault => -1f; // Model.Value.RunSpeed;

        protected override Vector3 DamageTextForce => new Vector3(0.0f, 0.8f);
        protected override Vector3 HudTextPosition => transform.TransformPoint(0f, 1f, 0f);

        protected override bool CanRun => base.CanRun && !TargetInAttackRange(_player);

        public CharacterSpineController SpineController { get; private set; }

        public override string TargetTag => Tag.Player;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            Animator = new EnemyAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = AnimatorTimeScale * Game.instance.Stage.AnimationTimeScaleWeight;

            TargetTag = Tag.Player;
        }

        private void OnDestroy()
        {
            Animator?.Dispose();
        }

        #endregion

        public override void Set(Model.CharacterBase model, bool updateCurrentHp = false)
        {
            if (!(model is Model.Enemy enemyModel))
                throw new ArgumentException(nameof(model));

            Set(enemyModel, _player, updateCurrentHp);
        }

        public void Set(Model.Enemy model, Player player, bool updateCurrentHP)
        {
            base.Set(model, updateCurrentHP);

            _disposablesForModel.DisposeAllAndClear();

            UpdateArmor();

            _player = player;

            StartRun();

            if (!ShowSpeech("ENEMY", model.RowData.Id))
            {
                ShowSpeech("ENEMY_INIT", model.spawnIndex);
            }
        }

        public override void UpdateActorHud()
        {
            base.UpdateActorHud();

            var boss = Game.instance.Stage.Boss;
            if (!(boss is null) && !Id.Equals(boss.Id))
                return;

            var battle = Widget.Find<UI.Battle>();
            battle.BossStatus.SetHp(CurrentHp, Hp);
            battle.BossStatus.SetBuff(CharacterModel.Buffs);
        }

        public override IEnumerator CoProcessDamage(Model.BattleStatus.Skill.SkillInfo info, bool isConsiderDie,
            bool isConsiderElementalType)
        {
            yield return StartCoroutine(base.CoProcessDamage(info, isConsiderDie, isConsiderElementalType));

            if (!IsDead)
                ShowSpeech("ENEMY_DAMAGE");
        }

        protected override IEnumerator Dying()
        {
            ShowSpeech("ENEMY_DEAD");
            yield return StartCoroutine(base.Dying());
        }

        protected override void OnDeadStart()
        {
            Event.OnEnemyDeadStart.Invoke(this);
            base.OnDeadStart();
        }

        protected override void OnDeadEnd()
        {
            base.OnDeadEnd();

            if (Animator.Target != null)
            {
                Animator.DestroyTarget();
            }
        }

        protected override BoxCollider GetAnimatorHitPointBoxCollider()
        {
            return SpineController.BoxCollider;
        }

        #region AttackPoint & HitPoint

        protected override void UpdateHitPoint()
        {
            base.UpdateHitPoint();

            var center = HitPointBoxCollider.center;
            var size = HitPointBoxCollider.size;
            HitPointLocalOffset = new Vector3(center.x - size.x / 2, center.y - size.y / 2);
            attackPoint.transform.localPosition = new Vector3(HitPointLocalOffset.x - CharacterModel.attackRange, 0f);
        }

        #endregion

        #region Equipments & Customize

        private const int DefaultCharacter = 201000;

        private void UpdateArmor()
        {
            var armorId = CharacterModel?.RowData.Id ?? DefaultCharacter;
            ChangeSpineResource(armorId);
            UpdateHitPoint();
        }

        public void ChangeSpineResource(int id)
        {
            var key = id.ToString();
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
        }

        #endregion

        protected override void ProcessAttack(Actor target, Model.BattleStatus.Skill.SkillInfo skill, bool isLastHit,
            bool isConsiderElementalType)
        {
            ShowSpeech("ENEMY_SKILL", (int) skill.ElementalType, (int) skill.SkillCategory);
            base.ProcessAttack(target, skill, isLastHit, isConsiderElementalType);
            ShowSpeech("ENEMY_ATTACK");
        }

        protected override IEnumerator CoAnimationCast(Model.BattleStatus.Skill.SkillInfo info)
        {
            ShowSpeech("ENEMY_SKILL", (int) info.ElementalType, (int) info.SkillCategory);
            yield return StartCoroutine(base.CoAnimationCast(info));
        }

        public async UniTask WinAsync(float animDuration)
        {
            if (isActiveAndEnabled)
            {
                Animator.Win();
            }

            await UniTask.Delay(TimeSpan.FromSeconds(animDuration));

            if (Animator.Target != null)
            {
                Animator.DestroyTarget();
            }
        }

        public override void SetSpineColor(Color color, int propertyID = -1)
        {
            if (SpineController == null)
            {
                return;
            }

            var skeletonAnimation = SpineController.SkeletonAnimation;
            if (skeletonAnimation == null)
            {
                return;
            }

            base.SetSpineColor(color, propertyID);

            if (skeletonAnimation.TryGetComponent<MeshRenderer>(out var meshRenderer))
            {
                var mpb = new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(mpb);
                mpb.SetColor(propertyID, color);
                meshRenderer.SetPropertyBlock(mpb);
            }
            else
            {
                NcDebug.LogError($"[{nameof(StageMonster)}] No MeshRenderer found in {name}.");
            }
        }
    }
}
