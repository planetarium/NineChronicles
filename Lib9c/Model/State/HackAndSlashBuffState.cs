using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.TableData.Crystal;
using System.Linq;
using Libplanet;
using Nekoyume.TableData;

namespace Nekoyume.Model.State
{
    public class HackAndSlashBuffState : IState
    {
        public Address Address { get; }
        public int StageId { get; }
        public int StarCount { get; private set; }
        public List<int> BuffIds { get; private set; }

        public HackAndSlashBuffState(Address address, int stageId)
        {
            Address = address;
            StageId = stageId;
            StarCount = 0;
            BuffIds = new List<int>();
        }

        public HackAndSlashBuffState(Address address, List serialized)
        {
            Address = address;
            StageId = serialized[0].ToInteger();
            StarCount = serialized[1].ToInteger();
            BuffIds = serialized[2].ToList(StateExtensions.ToInteger);
        }

        public void Update(int gotStarCount, CrystalStageBuffGachaSheet sheet)
        {
            StarCount = Math.Min(StarCount + gotStarCount, sheet[StageId].MaxStar);
        }

        public void Update(List<int> buffIds)
        {
            BuffIds = buffIds;
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(StageId.Serialize())
                .Add(StarCount.Serialize())
                .Add(BuffIds.Select(i => i.Serialize()).Serialize());
        }

        public static Skill.BuffSkill GetBuffSkill(
            List<int> buffIds,
            int? buffId,
            CrystalRandomBuffSheet crystalRandomBuffSheet,
            SkillSheet skillSheet)
        {
            int selectedId;
            if (buffId.HasValue && buffIds.Contains(buffId.Value))
            {
                selectedId = buffId.Value;
            }
            else
            {
                selectedId = buffIds
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

            return new Skill.BuffSkill(skillRow, 0, 100);
        }
    }
}
