using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    public interface IStage
    {
        IEnumerator CoSpawnPlayer(Player character);
        IEnumerator CoAttack(CharacterBase character, Attack.AttackInfo attack);
        IEnumerator CoAreaAttack(CharacterBase character, List<Attack.AttackInfo> attacks);
        IEnumerator CoDropBox(List<ItemBase> items);
        IEnumerator CoGetReward(List<ItemBase> rewards);
        IEnumerator CoSpawnWave(List<Monster> monsters, bool isBoss);
        IEnumerator CoGetExp(long exp);
    }
}
