using System;
using System.Collections;
using UniRx;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class Enemy : CharacterBase
    {
        protected override Vector3 DamageTextForce => new Vector3(0.1f, 0.5f);

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
        }
        
        public override IEnumerator CoProcessDamage(Model.Skill.SkillInfo info)
        {
            yield return StartCoroutine(base.CoProcessDamage(info));
            var position = transform.TransformPoint(0f, 1f, 0f);
            var force = DamageTextForce;
            animator.Hit();
            PopUpDmg(position, force, info);

        }
        
        protected override bool CanRun()
        {
            return base.CanRun() && !TargetInRange(_player);
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
    }
}
