using System;
using Assets.SimpleLocalization;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class WorldQuest : Quest
    {
        private readonly int _goal;
        public WorldQuest(WorldQuestSheet.Row data) : base(data)
        {
            _goal = data.Goal;
        }

        public override void Check()
        {
        }

        public override string ToInfo()
        {
            if (Game.instance.TableSheets.WorldSheet.TryGetByStageId(_goal, out var worldRow))
            {
                var format = LocalizationManager.Localize("QUEST_WORLD_FORMAT");
                return string.Format(format, worldRow.GetLocalizedName());
            }
            throw new SheetRowNotFoundException("WorldSheet", "TryGetByStageId()", _goal.ToString());
        }

        public void Update(CollectionMap stageMap)
        {
            Complete = stageMap.TryGetValue(_goal, out _);
        }
    }
}
