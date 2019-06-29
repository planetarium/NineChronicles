using System;
using System.Collections;
using UniRx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Enemy : CharacterBase
    {
        protected override Vector3 DamageTextForce => new Vector3(0.0f, 0.8f);

        public Guid id;
        public override Guid Id => id;
        
        public int DataId = 0;

        private Player _player;

        public override float Speed => _runSpeed;
        
        protected override Vector3 HUDOffset => animator.GetHUDPosition();
        private float _runSpeed = -1.0f;

        #region Mono

        protected override void Awake()
        {
            base.Awake();
            
            animator = new EnemyAnimator(this);
            animator.OnEvent.Subscribe(OnAnimatorEvent);
            animator.TimeScale = AnimatorTimeScale;
            
            targetTag = Tag.Player;
        }

        private void OnDestroy()
        {
            animator?.Dispose();
        }

        #endregion
        
        public void Init(Model.Monster spawnCharacter, Player player)
        {
            _player = player;
            InitStats(spawnCharacter);
            id = spawnCharacter.id;
            StartRun();

            if (!ShowSpeech("ENEMY", spawnCharacter.data.id))
            {
                ShowSpeech("ENEMY_INIT", spawnCharacter.spawnIndex);
            }
        }
        
        public override IEnumerator CoProcessDamage(Model.Skill.SkillInfo info)
        {
            yield return StartCoroutine(base.CoProcessDamage(info));
            var position = transform.TransformPoint(0f, 1f, 0f);
            var force = DamageTextForce;
            animator.Hit();
            PopUpDmg(position, force, info);

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
            StopRun();
            animator.Die();
            yield return new WaitForSeconds(1.2f);
            DisableHUD();
            yield return new WaitForSeconds(.8f);
            OnDead();
        }

        protected override void OnDead()
        {
            Event.OnEnemyDead.Invoke(this);
            base.OnDead();
        }
        
        private void InitStats(Model.Monster character)
        {
            var stats = character.data.GetStats(character.level);
            HP = stats.HP;
            ATK = stats.Damage;
            DEF = stats.Defense;
            Power = 0;
            HPMax = HP;
            Range = character.attackRange;
            _runSpeed = -character.runSpeed;
            characterSize = character.characterSize;
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

        protected override void ProcessAttack(CharacterBase target, Model.Skill.SkillInfo skill)
        {
            ShowSpeech("ENEMY_SKILL", (int)(skill.Elemental ?? 0), (int)skill.Category);
            base.ProcessAttack(target, skill);
            ShowSpeech("ENEMY_ATTACK");
        }

        protected override IEnumerator CoAnimationCast(Model.Skill.SkillInfo info)
        {
            ShowSpeech("ENEMY_SKILL", (int)(info.Elemental ?? 0), (int)info.Category);
            yield return StartCoroutine(base.CoAnimationCast(info));
        }
    }
}
