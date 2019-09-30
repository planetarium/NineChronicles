using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.TableData;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class TradeQuest : Quest
    {
        private int _current;
        
        public new TradeQuestSheet.Row Data { get; }

        public TradeQuest(TradeQuestSheet.Row data) : base(data)
        {
            Data = data;
        }

        public override void Check(Player player, List<ItemBase> items)
        {
            if (Complete)
                return;
            _current += 1;
            Complete = _current >= Data.Goal;
        }

        public override string ToInfo()
        {
            return LocalizationManager.LocalizeTradeQuest(Data.Type, _current, Data.Goal);
        }
    }
}
