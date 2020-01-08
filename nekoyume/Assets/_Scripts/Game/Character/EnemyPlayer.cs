using Nekoyume.UI;

namespace Nekoyume.Game.Character
{
    public class EnemyPlayer : Player
    {
        public override void UpdateHpBar()
        {
            base.UpdateHpBar();

            var battle = Widget.Find<UI.Battle>();
            battle.enemyPlayerStatus.SetHp(CurrentHP, HP);
            battle.enemyPlayerStatus.SetBuff(Model?.Buffs);
        }
    }
}
