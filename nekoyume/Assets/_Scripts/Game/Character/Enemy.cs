using Nekoyume.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Enemy : CharacterBase
    {
        private Player _player;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public new readonly ReactiveProperty<Model.Enemy> Model = new ReactiveProperty<Model.Enemy>();

        // todo: 적의 이동속도에 따라서 인게임 연출 버그가 발생할 수 있으니 '-1f'로 값을 고정함. 이후 이 문제를 해결해서 몬스터 별 이동속도를 구현할 필요가 있음.
        protected override float RunSpeedDefault => -1f; // Model.Value.RunSpeed;

        protected Vector3 DamageTextForce => new Vector3(0.0f, 0.8f);

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            Animator = new EnemyAnimator(this);
            Animator.OnEvent.Subscribe(OnAnimatorEvent);
            Animator.TimeScale = AnimatorTimeScale;

            TargetTag = Tag.Player;
        }

        private void OnDestroy()
        {
            Animator?.Dispose();
        }

        #endregion

        public override void Set(Model.CharacterBase model, bool updateCurrentHP = false)
        {
            if (!(model is Model.Enemy enemyModel))
                throw new ArgumentException(nameof(model));

            Set(enemyModel, _player, updateCurrentHP);
        }

        public void Set(Model.Enemy model, Player player, bool updateCurrentHP)
        {
            base.Set(model, updateCurrentHP);

            _disposablesForModel.DisposeAllAndClear();
            Model.SetValueAndForceNotify(model);

            _player = player;

            StartRun();

            if (!ShowSpeech("ENEMY", model.RowData.Id))
            {
                ShowSpeech("ENEMY_INIT", model.spawnIndex);
            }
        }

        public override void UpdateHpBar()
        {
            base.UpdateHpBar();

            var boss = Game.instance.stage.Boss;
            if (!(boss is null) && !Id.Equals(boss.Id))
                return;

            var battle = Widget.Find<UI.Battle>();
            battle.bossStatus.SetHp(CurrentHP, HP);
        }

        protected override IEnumerator CoProcessDamage(Model.Skill.SkillInfo info, bool isConsiderDie,
            bool isConsiderElementalType)
        {
            yield return StartCoroutine(base.CoProcessDamage(info, isConsiderDie, isConsiderElementalType));
            var position = transform.TransformPoint(0f, 1f, 0f);
            var force = DamageTextForce;
            PopUpDmg(position, force, info, isConsiderElementalType);

            if (!IsDead())
                ShowSpeech("ENEMY_DAMAGE");
        }

        protected override bool CanRun()
        {
            return base.CanRun() && !TargetInRange(_player);
        }

        protected override IEnumerator Dying()
        {
            ShowSpeech("ENEMY_DEAD");
            yield return StartCoroutine(base.Dying());
        }

        protected override void OnDead()
        {
            Event.OnEnemyDead.Invoke(this);
            base.OnDead();
        }

        private void OnAnimatorEvent(string eventName)
        {
            switch (eventName)
            {
                case "attackStart":
                    break;
                case "attackPoint":
                    Event.OnAttackEnd.Invoke(this);
                    break;
                case "footstep":
                    break;
            }
        }

        protected override void ProcessAttack(CharacterBase target, Model.Skill.SkillInfo skill, bool isLastHit,
            bool isConsiderElementalType)
        {
            ShowSpeech("ENEMY_SKILL", (int) skill.ElementalType, (int) skill.SkillCategory);
            base.ProcessAttack(target, skill, isLastHit, isConsiderElementalType);
            ShowSpeech("ENEMY_ATTACK");
        }

        protected override IEnumerator CoAnimationCast(Model.Skill.SkillInfo info)
        {
            ShowSpeech("ENEMY_SKILL", (int) info.ElementalType, (int) info.SkillCategory);
            yield return StartCoroutine(base.CoAnimationCast(info));
        }
    }
}
