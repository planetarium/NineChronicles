using System.Collections.Generic;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.UI;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.State.Modifiers
{
    public class AvatarWorldInformationAddWorldModifier : AvatarStateModifier
    {
        [SerializeField]
        private readonly List<int> _worldIds;

        public override bool IsEmpty => _worldIds.Count == 0;

        public AvatarWorldInformationAddWorldModifier(params int[] worldIds)
        {
            _worldIds = new List<int>(worldIds);
        }

        public override void Add(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarWorldInformationAddWorldModifier m))
            {
                return;
            }

            foreach (var incoming in m._worldIds.Where(incoming => !_worldIds.Contains(incoming)))
            {
                _worldIds.Add(incoming);
            }
        }

        public override void Remove(IAccumulatableStateModifier<AvatarState> modifier)
        {
            if (!(modifier is AvatarWorldInformationAddWorldModifier m))
            {
                return;
            }

            foreach (var incoming in m._worldIds.Where(incoming => _worldIds.Contains(incoming)))
            {
                _worldIds.Remove(incoming);
            }
        }

        public override AvatarState Modify(AvatarState state)
        {
            var wi = state.worldInformation;
            var worldSheet = Game.Game.instance.TableSheets.WorldSheet;
            foreach (var worldId in _worldIds)
            {
                if (wi.TryGetWorld(worldId, out _))
                {
                    continue;
                }

                var worldRow = worldSheet.OrderedList.FirstOrDefault(row => row.Id == worldId);
                if (worldRow is null)
                {
                    NotificationSystem.Push(MailType.System,
                        L10nManager.Localize("ERROR_WORLD_DOES_NOT_EXIST"),
                        NotificationCell.NotificationType.Information);
                    continue;
                }

                if (!wi.TryAddWorld(worldRow, out _))
                {
                    continue;
                }

                var worldUnlockSheetRow = Game.Game.instance.TableSheets.WorldUnlockSheet
                    .OrderedList
                    .FirstOrDefault(row => row.WorldIdToUnlock == worldId);
                if (!(worldUnlockSheetRow is null) &&
                    wi.IsWorldUnlocked(worldUnlockSheetRow.WorldId) &&
                    wi.IsStageCleared(worldUnlockSheetRow.StageId))
                {
                    wi.UnlockWorld(worldId, Game.Game.instance.Agent.BlockIndex, worldSheet);
                }
            }

            return state;
        }
    }
}
