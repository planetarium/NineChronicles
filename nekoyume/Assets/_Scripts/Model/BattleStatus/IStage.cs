namespace Nekoyume.Model
{
    public interface IStage
    {
        void SpawnPlayer(Player character);
        void SpawnMonster(Monster character);
        void StageEnter(int stage);
        void StageEnd(BattleLog.Result result);
        void Dead(CharacterBase character);
        void Attack(int atk, CharacterBase character, CharacterBase target, bool critical);
        void DropItem(Monster character);
    }
}
