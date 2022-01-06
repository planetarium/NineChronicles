using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bencodex.Types;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    [Serializable]
    public class AvatarQuestIsReceivableSetter : AvatarStateModifier
    {
        [SerializeField]
        private List<int> questIdList;

        public override bool IsEmpty => !questIdList.Any();

        public AvatarQuestIsReceivableSetter(params int[] questIdParams)
        {
            questIdList = new List<int>();
            foreach (var id in questIdParams)
            {
                questIdList.Add(id);
            }
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarQuestIsReceivableSetter m))
            {
                return;
            }

            foreach (var incoming in m.questIdList.Where(incoming =>
                !questIdList.Contains(incoming)))
            {
                questIdList.Add(incoming);
            }
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarQuestIsReceivableSetter m))
            {
                return;
            }

            foreach (var incoming in m.questIdList.Where(incoming =>
                questIdList.Contains(incoming)))
            {
                questIdList.Remove(incoming);
            }
        }

        public override AvatarState Modify(AvatarState state)
        {
            if (state is null)
            {
                return null;
            }

            var quests = state.questList;
            foreach (var quest in quests)
            {
                foreach (var id in questIdList)
                {
                    if (id.Equals(quest.Id))
                    {
                        quest.isReceivable = true;
                    }
                }
            }

            return state;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var id in questIdList)
            {
                sb.AppendLine(id.ToString());
            }

            return sb.ToString();
        }
    }
}
