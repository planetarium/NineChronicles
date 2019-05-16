using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    public interface IStage
    {
        IEnumerator CoSpawnPlayer(Player character);
        IEnumerator CoAttack(CharacterBase caster, IEnumerable<Skill.SkillInfo> infos);
        IEnumerator CoHeal(CharacterBase caster, IEnumerable<Skill.SkillInfo> infos);
        IEnumerator CoDropBox(List<ItemBase> items);
        IEnumerator CoGetReward(List<ItemBase> rewards);
        IEnumerator CoSpawnWave(List<Monster> monsters, bool isBoss);
        IEnumerator CoGetExp(long exp);
    }
}
