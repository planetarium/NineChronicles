using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Item;
using Nekoyume.Model;

namespace Nekoyume.Game.Quest
{
    [Serializable]
    public class TradeQuest : Quest
    {
        private int _current;
        public readonly string type;

        public TradeQuest(Data.Table.Quest data) : base(data)
        {
            var tradeData = (Data.Table.TradeQuest) data;
            type = tradeData.type;
        }

        public override void Check(Player player, List<ItemBase> items)
        {
            if (Complete)
                return;
            _current += 1;
            Complete = _current >= goal;
        }

        public override string ToInfo()
        {
            return LocalizationManager.LocalizeTradeQuest(type, _current, goal);
        }
    }
}
