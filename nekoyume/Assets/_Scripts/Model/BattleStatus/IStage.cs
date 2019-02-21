using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    public interface IStage
    {
        void SpawnPlayer();
        void SpawnMonster(Monster character);
        void StageEnter(int stage);
        void StageEnd(BattleLog.Result result);
        void Dead(CharacterBase character);
        void Attack(int atk, CharacterBase character, CharacterBase target);
        void DropItem(Monster character);
    }
}
