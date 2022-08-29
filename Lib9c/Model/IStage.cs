using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.Item;

namespace Nekoyume.Model
{
    public interface IStage
    {
        IEnumerator CoSpawnPlayer(Player character);
        IEnumerator CoSpawnEnemyPlayer(EnemyPlayer character);

        #region Skill

        IEnumerator CoNormalAttack(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoBlowAttack(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoDoubleAttack(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoAreaAttack(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoBuffRemovalAttack(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoHeal(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoBuff(CharacterBase caster, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos, IEnumerable<BattleStatus.Skill.SkillInfo> buffInfos);
        IEnumerator CoTickDamage(CharacterBase affectedCharacter, int skillId, IEnumerable<BattleStatus.Skill.SkillInfo> skillInfos);
        
        #endregion
        
        IEnumerator CoRemoveBuffs(CharacterBase caster);
        
        IEnumerator CoDropBox(List<ItemBase> items);
        IEnumerator CoGetReward(List<ItemBase> rewards);
        IEnumerator CoSpawnWave(int waveNumber, int waveTurn, List<Enemy> enemies, bool hasBoss);
        IEnumerator CoGetExp(long exp);
        IEnumerator CoWaveTurnEnd(int turnNumber, int waveTurn);
        IEnumerator CoDead(CharacterBase character);
    }
}
