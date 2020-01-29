using System;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.TableData;

namespace Nekoyume.Model.Quest
{
    [Serializable]
    public class WorldQuest : Quest
    {
        public WorldQuest(WorldQuestSheet.Row data) : base(data)
        {
        }

        public WorldQuest(Dictionary serialized) : base(serialized)
        {
        }

        public override QuestType QuestType => QuestType.Adventure;

        public override void Check()
        {
        }

        public override string GetName()
        {
            if (Game.Game.instance.TableSheets.WorldSheet.TryGetByStageId(Goal, out var worldRow))
            {
                var format = LocalizationManager.Localize("QUEST_WORLD_FORMAT");
                return string.Format(format, worldRow.GetLocalizedName());
            }
            throw new SheetRowNotFoundException("WorldSheet", "TryGetByStageId()", Goal.ToString());
        }

        public override string GetProgressText()
        {
            return string.Empty;
        }

        protected override string TypeId => "worldQuest";

        public void Update(CollectionMap stageMap)
        {
            if (Complete)
                return;

            Complete = stageMap.TryGetValue(Goal, out _);
        }
    }
}
