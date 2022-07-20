using Nekoyume.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    using Nekoyume.Model;
    using UniRx;

    public class RaidBoss : RaidCharacter
    {
        [SerializeField]
        private SpineController controller;

        protected override void Awake()
        {
            Animator = new EnemyAnimator(this)
            {
                TimeScale = AnimatorTimeScale
            };

            base.Awake();
        }

        public override void Init(RaidCharacter target)
        {
            Animator.ResetTarget(gameObject);
            _attackTime = SpineAnimationHelper.GetAnimationDuration(controller, "Attack");
            _criticalAttackTime = SpineAnimationHelper.GetAnimationDuration(controller, "CriticalAttack");

            base.Init(target);
        }

        public override void UpdateStatusUI()
        {
            base.UpdateStatusUI();
            _raidBattle.UpdateStatus(_currentHp, _characterModel.HP, _characterModel.Buffs);
        }
    }
}
