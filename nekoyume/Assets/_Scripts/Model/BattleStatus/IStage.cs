using System.Collections.Generic;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    public interface IStage
    {
        void SpawnPlayer(Player character);
        void SpawnMonster(Monster character);
        void Dead(CharacterBase character);
        void Attack(int atk, CharacterBase character, CharacterBase target, bool critical);
        void DropBox(List<ItemBase> items);
    }
}
