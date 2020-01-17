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
            battle.enemyPlayerStatus.SetBuff(CharacterModel.Buffs);
        }

        public override void Set(Model.CharacterBase model, bool updateCurrentHP = false)
        {
            base.Set(model, updateCurrentHP);
            InitBT();
        }
    }
}
