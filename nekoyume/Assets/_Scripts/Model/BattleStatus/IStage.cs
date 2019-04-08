using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    public interface IStage
    {
        IEnumerator CoSpawnPlayer(Player character);
        IEnumerator CoAttack(int atk, CharacterBase character, CharacterBase target, bool critical);
        IEnumerator CoDropBox(List<ItemBase> items);
        IEnumerator CoGetReward(List<ItemBase> rewards);
        IEnumerator CoSpawnWave(List<Monster> monsters);
        IEnumerator CoGetExp(long exp);
    }
}
