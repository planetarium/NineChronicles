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
    public class HackAndSlashBuffState : IState
    {
        public Address Address { get; }
        public int StageId { get; }
        public int StarCount { get; private set; }
        public List<int> SkillIds { get; private set; }

        public HackAndSlashBuffState(Address address, int stageId)
        {
            Address = address;
            StageId = stageId;
            StarCount = 0;
            SkillIds = new List<int>();
        }

        public HackAndSlashBuffState(Address address, List serialized)
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
            List<int> skillIds,
            int? skillId,
            CrystalRandomBuffSheet crystalRandomBuffSheet,
            SkillSheet skillSheet)
        {
            int selectedId;
            if (skillId.HasValue && skillIds.Contains(skillId.Value))
            {
                selectedId = skillId.Value;
            }
            else
            {
                selectedId = skillIds
                    .OrderBy(id => crystalRandomBuffSheet[id].Rank)
                    .ThenBy(id => id)
                    .First();
            }

            if (!crystalRandomBuffSheet.TryGetValue(selectedId, out var row))
            {
                throw new SheetRowNotFoundException(nameof(CrystalRandomBuffSheet), selectedId);
            }

            if (!skillSheet.TryGetValue(row.SkillId, out var skillRow))
            {
                throw new SheetRowNotFoundException(nameof(SkillSheet), row.SkillId);
            }

            return SkillFactory.Get(skillRow, 0, 100);
        }
    }
}
