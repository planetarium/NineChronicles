using System;
using System.Collections.Generic;
using Nekoyume.TableData;

namespace Nekoyume.Model.Buff
{
    [Serializable]
    public abstract class ActionBuff : Buff
    {
        public ActionBuffSheet.Row RowData { get; }

        public ActionBuff(ActionBuffSheet.Row row) : base(
            new BuffInfo(row.Id, row.GroupId, row.Chance, row.Duration, row.TargetType))
        {
            RowData = row;
        }

        protected ActionBuff(ActionBuff value) : base(value)
        {
            RowData = value.RowData;
        }

        public abstract BattleStatus.Skill GiveEffect(
            CharacterBase caster,
            int simulatorWaveTurn);
    }
}
