﻿using System.Linq;
using Libplanet.Types.Assets;
using Nekoyume.State;
using Nekoyume.TableData;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class RuneItem
    {
        public RuneListSheet.Row Row { get; }
        public RuneOptionSheet.Row OptionRow { get; }
        public RuneCostSheet.RuneCostData Cost { get; }

        public FungibleAssetValue RuneStone { get; set; }
        public int Level { get; }
        public bool IsMaxLevel { get; }
        public bool EnoughRuneStone { get; }
        public bool EnoughCrystal { get; }
        public bool EnoughNcg { get; }
        public bool HasNotification => EnoughRuneStone && EnoughCrystal && EnoughNcg;

        public readonly ReactiveProperty<bool> IsSelected = new();

        public RuneItem(RuneListSheet.Row row, int level)
        {
            Row = row;
            Level = level;

            var runeOptionSheet = Game.Game.instance.TableSheets.RuneOptionSheet;
            if (!runeOptionSheet.TryGetValue(row.Id, out var optionRow))
            {
                return;
            }

            OptionRow = optionRow;

            IsMaxLevel = level == optionRow.LevelOptionMap.Count;

            var runeRow = Game.Game.instance.TableSheets.RuneSheet[row.Id];
            if (!States.Instance.CurrentAvatarBalances.ContainsKey(runeRow.Ticker))
            {
                return;
            }

            RuneStone = States.Instance.CurrentAvatarBalances[runeRow.Ticker];

            var costSheet = Game.Game.instance.TableSheets.RuneCostSheet;
            if (!costSheet.TryGetValue(row.Id, out var costRow))
            {
                return;
            }

            Cost = costRow.Cost.FirstOrDefault(x => x.Level == level + 1);
            if (Cost is null)
            {
                if (IsMaxLevel)
                {
                    EnoughRuneStone = false;
                    EnoughCrystal = false;
                    EnoughNcg = false;
                }
                return;
            }

            EnoughRuneStone = RuneStone.MajorUnit >= Cost.RuneStoneQuantity;
            EnoughCrystal = States.Instance.AgentCrystal.MajorUnit >= Cost.CrystalQuantity;
            EnoughNcg = States.Instance.AgentNCG.MajorUnit >= Cost.NcgQuantity;
        }
    }
}
