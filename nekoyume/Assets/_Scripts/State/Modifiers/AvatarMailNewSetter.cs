using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nekoyume.JsonConvertibles;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarMailNewSetter : AvatarStateModifier
    {
        [SerializeField] protected List<JsonConvertibleGuid> guidList;

        public override bool IsEmpty => !guidList.Any();

        public AvatarMailNewSetter(params Guid[] guidParams)
        {
            guidList = new List<JsonConvertibleGuid>();
            foreach (var guid in guidParams)
            {
                guidList.Add(new JsonConvertibleGuid(guid));
            }
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarMailNewSetter m))
            {
                return;
            }

            foreach (var incoming in m.guidList.Where(incoming =>
                !guidList.Contains(incoming)))
            {
                guidList.Add(incoming);
            }
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarMailNewSetter m))
            {
                return;
            }

            foreach (var incoming in m.guidList.Where(incoming =>
                guidList.Contains(incoming)))
            {
                guidList.Remove(incoming);
            }
        }

        public override AvatarState Modify(AvatarState state)
        {
            if (state is null)
            {
                return null;
            }

            var ids = new HashSet<Guid>(guidList.Select(i => i.Value));
            var attachmentMails = state.mailBox
                .OfType<Mail>()
                .Where(m => ids.Contains(m.id));
            foreach (var attachmentMail in attachmentMails)
            {
                attachmentMail.New = true;
            }

            return state;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var guid in guidList)
            {
                sb.AppendLine(guid.Value.ToString());
            }

            return sb.ToString();
        }
    }
}
