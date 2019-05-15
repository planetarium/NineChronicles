using System.Collections;
using System.Collections.Generic;
using Nekoyume.Data.Table;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    public interface IStage
    {
        IEnumerator CoSpawnPlayer(Player character);
        IEnumerator CoSkill(CharacterBase caster, SkillEffect.SkillType type, IEnumerable<Skill.SkillInfo> skills);
        IEnumerator CoDropBox(List<ItemBase> items);
        IEnumerator CoGetReward(List<ItemBase> rewards);
        IEnumerator CoSpawnWave(List<Monster> monsters, bool isBoss);
        IEnumerator CoGetExp(long exp);
    }
}
