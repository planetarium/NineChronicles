using System;
using Assets.SimpleLocalization;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class BattleQuest : Quest
    {
        public override QuestType QuestType => QuestType.Adventure;

        private int _worldStage;
        public BattleQuest(BattleQuestSheet.Row data) : base(data)
        {
        }

        public override void Check()
        {
            if (Complete)
                return;
            Complete = _worldStage > Data.Goal;
        }

        public override string ToInfo()
        {
            var format = LocalizationManager.Localize("QUEST_BATTLE_CURRENT_INFO_FORMAT");
            return string.Format(format, Data.Goal);
        }

        public void Update(int stage)
        {
            _worldStage = stage;
            Check();
        }
    }
}
