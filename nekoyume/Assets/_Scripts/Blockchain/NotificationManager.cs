using System.Collections.Generic;
using System.Linq;
using Lib9c.Renderers;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI;
using Nekoyume.UI.Scroller;

namespace Nekoyume.Blockchain
{
    public static class NotificationManager
    {
        private static readonly TableSheets TableSheets = Game.Game.instance.TableSheets;

        private static void Notify(string message)
        {
            NotificationSystem.Push(
                MailType.System,
                message,
                NotificationCell.NotificationType.Notification);
        }

        public static IEnumerable<RuneListSheet.Row> FilterRuneSummon(
            ActionEvaluation<RuneSummon> eval)
        {
            var summonSheet = TableSheets.SummonSheet;
            var runeSheet = TableSheets.RuneSheet;
            var runeList = TableSheets.RuneListSheet;
            var groupId = eval.Action.GroupId;
            var summonRow = summonSheet.OrderedList!.FirstOrDefault(row => row.GroupId == groupId);
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var result =
                RuneSummon.SimulateSummon(runeSheet, summonRow, eval.Action.SummonCount, random);

            return result.Keys
                .Select(rune => runeSheet.OrderedList!.First(r => r.Ticker == rune.Ticker))
                .Select(rn => runeList.OrderedList!.First(r => r.Id == rn.Id))
                .Where(runeData => runeData.Grade == 5);
        }

        public static void NotifyRuneSummon(ActionEvaluation<RuneSummon> eval)
        {
            var avatarName = StateGetter.GetAvatarState(eval.Action.AvatarAddress, eval.OutputState).name;
            var runeSheet = TableSheets.RuneSheet;
            var runeList = FilterRuneSummon(eval);
            foreach (var rune in runeList)
            {
                var runeData = runeSheet.OrderedList!.First(r => r.Id == rune.Id);
                Notify($"{avatarName} Summoned Legendary {runeData.Ticker}");
            }
        }

        public static IEnumerable<Equipment> FilterAuraSummon(ActionEvaluation<AuraSummon> eval)
        {
            var summonSheet = TableSheets.SummonSheet;
            var groupId = eval.Action.GroupId;
            var summonRow = summonSheet.OrderedList!.FirstOrDefault(row => row.GroupId == groupId);
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var result = AuraSummon.SimulateSummon(
                States.Instance.CurrentAvatarState.address.ToString(),
                States.Instance.AgentState,
                TableSheets.EquipmentItemRecipeSheet,
                TableSheets.EquipmentItemSheet,
                TableSheets.EquipmentItemSubRecipeSheetV2,
                TableSheets.EquipmentItemOptionSheet,
                TableSheets.SkillSheet,
                summonRow,
                eval.Action.SummonCount,
                random,
                eval.BlockIndex
            ).Where(r => r.Item2.Grade == 5).Select(r => r.Item2);
            return result;
        }

        public static void NotifyAuraSummon(ActionEvaluation<AuraSummon> eval)
        {
            var avatarName = StateGetter.GetAvatarState(eval.Action.AvatarAddress, eval.OutputState).name;
            foreach (var aura in FilterAuraSummon(eval))
            {
                if (aura.Grade == 5)
                {
                    Notify($"{avatarName} Summoned Legendary {aura.Id}");
                }
            }
        }
    }
}
