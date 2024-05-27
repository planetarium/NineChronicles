using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game.Character
{
    public class EnemyPlayer : Player
    {
        private Player _player;

        protected override bool CanRun =>
            !TargetInAttackRange(_player) && !_player.TargetInAttackRange(this);

        public override string TargetTag => Tag.Player;

        public override void UpdateActorHud()
        {
            base.UpdateActorHud();

            var battle = Widget.Find<UI.Battle>();
            battle.EnemyPlayerStatus.SetHp(CurrentHp, Hp);
            battle.EnemyPlayerStatus.SetBuff(CharacterModel.Buffs);
        }

        public void Set(Model.CharacterBase model, Player player, bool updateCurrentHP = false)
        {
            base.Set(model, updateCurrentHP);
            _player = player;
            InitBT();

            IsFlipped = true;
        }

        protected override void UpdateHitPoint()
        {
            base.UpdateHitPoint();

            var center = HitPointBoxCollider.center;
            var size = HitPointBoxCollider.size;
            HitPointLocalOffset = new Vector3(center.x - size.x / 2, center.y - size.y / 2);
            attackPoint.transform.localPosition = new Vector3(HitPointLocalOffset.x - CharacterModel.attackRange, 0f);
        }

        public override float CalculateRange(Actor target)
        {
            var attackRangeStartPosition = gameObject.transform.position.x + HitPointLocalOffset.x;
            var targetHitPosition = target.transform.position.x + target.HitPointLocalOffset.x;
            return attackRangeStartPosition - targetHitPosition;
        }

        protected override void ExecuteRun()
        {
            Animator.Run();

            Vector2 position = transform.position;
            position.x += Time.deltaTime * -RunSpeed;
            transform.position = position;
        }
    }
}
