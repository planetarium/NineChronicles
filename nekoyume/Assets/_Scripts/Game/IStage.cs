using System.Collections;
using System.Collections.Generic;
using Nekoyume.Game.Item;

namespace Nekoyume.Model
{
    public interface IStage
    {
        IEnumerator CoSpawnPlayer(Player character);
        IEnumerator CoSpawnEnemyPlayer(EnemyPlayer character);

        #region Skill

        IEnumerator CoNormalAttack(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos, IEnumerable<Skill.SkillInfo> buffInfos);
        IEnumerator CoBlowAttack(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos, IEnumerable<Skill.SkillInfo> buffInfos);
        IEnumerator CoDoubleAttack(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos, IEnumerable<Skill.SkillInfo> buffInfos);
        IEnumerator CoAreaAttack(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos, IEnumerable<Skill.SkillInfo> buffInfos);
        IEnumerator CoHeal(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos, IEnumerable<Skill.SkillInfo> buffInfos);
        IEnumerator CoBuff(CharacterBase caster, IEnumerable<Skill.SkillInfo> skillInfos, IEnumerable<Skill.SkillInfo> buffInfos);
        
        #endregion
        
        IEnumerator CoRemoveBuffs(CharacterBase caster);
        
        IEnumerator CoDropBox(List<ItemBase> items);
        IEnumerator CoGetReward(List<ItemBase> rewards);
        IEnumerator CoSpawnWave(List<Enemy> enemies, bool isBoss);
        IEnumerator CoGetExp(long exp);
        IEnumerator CoWaveTurnEnd(int turn);
    }
}
