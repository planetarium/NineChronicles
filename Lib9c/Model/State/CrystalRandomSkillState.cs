using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.TableData.Crystal;
using System.Linq;
using Libplanet;
using Nekoyume.TableData;
using Nekoyume.Model.Skill;

namespace Nekoyume.Model.State
{
    public class CrystalRandomSkillState : IState
    {
        public Address Address { get; }
        public int StageId { get; }
        public int StarCount { get; private set; }
        public List<int> SkillIds { get; private set; }

        public CrystalRandomSkillState(Address address, int stageId)
        {
            Address = address;
            StageId = stageId;
            StarCount = 0;
            SkillIds = new List<int>();
        }

        public CrystalRandomSkillState(Address address, List serialized)
        {
            Address = address;
            StageId = serialized[0].ToInteger();
            StarCount = serialized[1].ToInteger();
            SkillIds = serialized[2].ToList(StateExtensions.ToInteger);
        }

        public void Update(int gotStarCount, CrystalStageBuffGachaSheet sheet)
        {
            StarCount = Math.Min(StarCount + gotStarCount, sheet[StageId].MaxStar);
        }

        public void Update(List<int> skillIds)
        {
            SkillIds = skillIds;
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(StageId.Serialize())
                .Add(StarCount.Serialize())
                .Add(SkillIds.Select(i => i.Serialize()).Serialize());
        }

        public static Skill.Skill GetSkill(
            int skillId,
            CrystalRandomBuffSheet crystalRandomBuffSheet,
            SkillSheet skillSheet)
        {
            if (!crystalRandomBuffSheet.TryGetValue(skillId, out var row))
            {
                throw new SheetRowNotFoundException(nameof(CrystalRandomBuffSheet), skillId);
            }

            if (!skillSheet.TryGetValue(row.SkillId, out var skillRow))
            {
                throw new SheetRowNotFoundException(nameof(SkillSheet), row.SkillId);
            }

            var isBuff = skillRow.SkillType == SkillType.Buff || skillRow.SkillType == SkillType.Debuff;
            if (!isBuff)
            {
                throw new ArgumentException($"Buff/Debuff skill is only supported for now. skillType : {skillRow.SkillType}");
            }

            return SkillFactory.Get(skillRow, default, 100);
        }

        public int GetHighestRankSkill(CrystalRandomBuffSheet crystalRandomBuffSheet)
        {
            return SkillIds
                .OrderBy(id => crystalRandomBuffSheet[id].Rank)
                .ThenBy(id => id)
                .First();
        }
    }
}
