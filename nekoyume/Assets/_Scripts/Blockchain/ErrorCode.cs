using System;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Types.Tx;
using Nekoyume.Action;
using Nekoyume.Exceptions;
using Nekoyume.L10n;
using Nekoyume.Model.Arena;
using Nekoyume.Model.Rune;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.Blockchain
{
    public static class ErrorCode
    {
        public static async UniTask<(string, string, string)> GetErrorCodeAsync(Exception exc)
        {
            var key = "ERROR_UNKNOWN";
            var code = "99";
            var errorMsg = string.Empty;
            switch (exc)
            {
                case RequiredBlockIndexException rb:
                    if (rb is RequiredBlockIntervalException _)
                    {
                        key = "ERROR_NO_CONTINUOUS_BATTLES";
                        code = "46";
                    }
                    else
                    {
                        key = "ERROR_REQUIRE_BLOCK";
                        code = "01";
                    }
                    break;
                case EquipmentSlotUnlockException _:
                    key = "ERROR_SLOT_UNLOCK";
                    code = "02";
                    break;
                case NotEnoughActionPointException _:
                    key = "ERROR_ACTION_POINT";
                    code = "03";
                    break;
                case InvalidAddressException _:
                    key = "ERROR_INVALID_ADDRESS";
                    code = "04";
                    break;
                case FailedLoadStateException _:
                    key = "ERROR_FAILED_LOAD_STATE";
                    code = "05";
                    break;
                case NotEnoughClearedStageLevelException _:
                    key = "ERROR_NoOT_ENOUGH_CLEARED_STAGE_LEVEL";
                    code = "06";
                    break;
                case WeeklyArenaStateAlreadyEndedException _:
                    key = "ERROR_WEEKLY_ARENA_STATE_ALREADY_ENDED";
                    code = "07";
                    break;
                case WeeklyArenaStateNotContainsAvatarAddressException _:
                    key = "ERROR_WEEKLY_ARENA_STATE_NOT_CONTAINS_AVATAR_ADDRESS";
                    code = "08";
                    break;
                case NotEnoughWeeklyArenaChallengeCountException _:
                    key = "ERROR_NOT_ENOUGH_WEEKLY_ARENA_CHALLENGE_COUNT";
                    code = "09";
                    break;
                case NotEnoughFungibleAssetValueException _:
                    key = "ERROR_NOT_ENOUGH_FUNGIBLE_ASSET_VALUE";
                    code = "10";
                    break;
                case SheetRowNotFoundException _:
                    code = "11";
                    break;
                case SheetRowColumnException _:
                    code = "12";
                    break;
                case InvalidWorldException _:
                    code = "13";
                    break;
                case InvalidStageException _:
                    code = "14";
                    break;
                case ConsumableSlotOutOfRangeException _:
                    code = "28";
                    break;
                case ConsumableSlotUnlockException _:
                    code = "15";
                    break;
                case DuplicateCostumeException _:
                    code = "16";
                    break;
                case InvalidItemTypeException _:
                    code = "17";
                    break;
                case CostumeSlotUnlockException _:
                    code = "18";
                    break;
                case NotEnoughMaterialException _:
                    code = "19";
                    break;
                case ItemDoesNotExistException _:
                    code = "20";
                    break;
                case InsufficientBalanceException _:
                    code = "21";
                    break;
                case FailedToUnregisterInShopStateException _:
                    code = "22";
                    break;
                case InvalidPriceException _:
                    code = "23";
                    break;
                case ShopStateAlreadyContainsException _:
                    code = "24";
                    break;
                case CombinationSlotResultNullException _:
                    code = "25";
                    break;
                case ActionTimeoutException ate:
                {
                    key = "ERROR_NETWORK";
                    errorMsg = "Action timeout occurred.";
                    TxId txId;
                    if (ate.TxId.HasValue)
                    {
                        txId = ate.TxId.Value;
                        if (await Game.Game.instance.Agent.IsTxStagedAsync(txId))
                        {
                            errorMsg += $" Transaction for action is still staged. (txId: {txId})";
                            code = "26";
                        }
                        else
                        {
                            errorMsg += $" Transaction for action is not staged. (txId: {txId})";
                            code = "27";
                        }
                    }
                    else if (ate.ActionId.HasValue)
                    {
                        if (Game.Game.instance.Agent.TryGetTxId(ate.ActionId.Value, out txId))
                        {
                            errorMsg += $" Transaction for action is still staged. (txId: {txId})";
                            code = "26";
                        }
                        else
                        {
                            errorMsg += " Transaction for action is not staged.";
                            code = "27";
                        }
                    }

                    NcDebug.LogError(
                        $"Action timeout: (txId: {txId}, actionId: {ate.ActionId}, code: {code})");

                    errorMsg += $"\nError Code: {code}";
                    break;
                }
                case UnableToRenderWhenSyncingBlocksException _:
                    code = "28";
                    key = "ERROR_UNABLE_TO_RENDER_WHEN_SYNCING_BLOCKS";
                    break;
                case NotEnoughAvatarLevelException _:
                    code = "29";
                    key = "ERROR_NOT_ENOUGH_AVATAR_LEVEL";
                    break;
                case RoundNotFoundException _:
                    code = "30";
                    key = "ERROR_ROUND_NOT_FOUND_EXCEPTION";
                    break;
                case ArenaScoreAlreadyContainsException _:
                    code = "31";
                    key = "ERROR_ARENA_SCORE_ALREADY_CONTAINS_EXCEPTION";
                    break;
                case ArenaInformationAlreadyContainsException _:
                    code = "31";
                    key = "ERROR_ARENA_INFORMATION_ALREADY_CONTAINS_EXCEPTION";
                    break;
                case ArenaParticipantsNotFoundException _:
                    code = "32";
                    key = "ERROR_ARENA_PARTICIPANTS_NOT_FOUND_EXCEPTION";
                    break;
                case ArenaAvatarStateNotFoundException _:
                    code = "33";
                    key = "ERROR_ARENA_AVATAR_STATE_NOT_FOUND_EXCEPTION";
                    break;
                case ArenaScoreNotFoundException _:
                    code = "34";
                    key = "ERROR_ARENA_SCORE_NOT_FOUND_EXCEPTION";
                    break;
                case ArenaInformationNotFoundException _:
                    code = "35";
                    key = "ERROR_ARENA_INFORMATION_NOT_FOUND_EXCEPTION";
                    break;
                case AddressNotFoundInArenaParticipantsException _:
                    code = "36";
                    key = "ERROR_ADDRESS_NOT_FOUND_IN_ARENA_PARTICIPANTS_EXCEPTION";
                    break;
                case NotEnoughTicketException _:
                    code = "37";
                    key = "ERROR_NOT_ENOUGH_TICKET_EXCEPTION";
                    break;
                case ValidateScoreDifferenceException _:
                    code = "38";
                    key = "ERROR_VALIDATE_SCORE_DIFFERENCE_EXCEPTION";
                    break;
                case ThisArenaIsClosedException _:
                    code = "39";
                    key = "ERROR_THIS_ARENA_IS_CLOSED_EXCEPTION";
                    break;
                case ExceedPlayCountException _:
                    code = "40";
                    key = "ERROR_EXCEED_PLAY_COUNT_EXCEPTION";
                    break;
                case ExceedTicketPurchaseLimitException _:
                    code = "41";
                    key = "ERROR_EXCEED_TICKET_PURCHASE_LIMIT_EXCEPTION";
                    break;
                case InvalidActionFieldException:
                {
                    code = "42";
                    key = "ERROR_INVALID_ACTION_FIELDS_EXCEPTION";
                    break;
                }
                case NotEnoughEventDungeonTicketsException:
                    code = "43";
                    key = "ERROR_NOT_ENOUGH_EVENT_DUNGEON_TICKETS_EXCEPTION";
                    break;
                case StageNotClearedException:
                    code = "44";
                    key = "ERROR_STAGE_NOT_CLEARED_EXCEPTION";
                    break;
                case CombinationSlotUnlockException:
                    code = "45";
                    key = "ERROR_COMBINATION_SLOT_UNLOCK_EXCEPTION";
                    break;
                case NotEnoughRankException _:
                    code = "46";
                    key = "NOT_ENOUGH_RANK_EXCEPTION";
                    break;
                case CoolDownBlockException _:
                    code = "47";
                    var battleArenaInterval = States.Instance.GameConfigState.BattleArenaInterval;
                    errorMsg = $"Arena battle is possible after at least {battleArenaInterval} blocks." +
                               $"\nError Code: {code}";
                    break;
                case MismatchRuneSlotTypeException _:
                    code = "48";
                    break;
                case RuneCostNotFoundException _:
                    code = "49";
                    break;
                case RuneCostDataNotFoundException _:
                    code = "50";
                    break;
                case RuneNotFoundException _:
                    code = "51";
                    break;
                case IsUsableSlotException _:
                    code = "52";
                    break;
                case SlotIsLockedException _:
                    code = "53";
                    break;
                case RuneListNotFoundException _:
                    code = "54";
                    break;
                case SlotRuneTypeException _:
                    code = "55";
                    break;
                case IsEquippableRuneException _:
                    code = "56";
                    break;
                case RuneStateNotFoundException _:
                    code = "57";
                    break;
                case SlotIsAlreadyUnlockedException _:
                    code = "58";
                    break;
                case SlotNotFoundException _:
                    code = "59";
                    break;
                case NotEnoughMedalException _:
                    code = "60";
                    break;
                case ExceedTicketPurchaseLimitDuringIntervalException _:
                    code = "61";
                    break;
                case DuplicatedRuneIdException _:
                    code = "62";
                    break;
                case DuplicatedRuneSlotIndexException _:
                    code = "63";
                    break;
                case RuneInfosIsEmptyException _:
                    code = "64";
                    break;
                case PlayCountIsZeroException _:
                    code = "65";
                    break;
                case TicketPurchaseLimitExceedException _:
                    code = "66";
                    break;
            }

            Analyzer.Instance.Track(
                "Unity/Error",
                ("code", code),
                ("key", key),
                ("AgentAddress", Game.Game.instance.Agent.Address.ToString()),
                ("AvatarAddress", Game.Game.instance.States.CurrentAvatarState.address.ToString()));

            var evt = new AirbridgeEvent("Error");
            evt.SetValue(Convert.ToInt16(code));
            evt.AddCustomAttribute("key", key);
            evt.AddCustomAttribute("agent-address", Game.Game.instance.Agent.Address.ToString());
            evt.AddCustomAttribute("avatar-address", Game.Game.instance.States.CurrentAvatarState.address.ToString());
            AirbridgeUnity.TrackEvent(evt);

            errorMsg = errorMsg == string.Empty
                ? string.Format(
                    L10nManager.Localize("UI_ERROR_RETRY_FORMAT"),
                    L10nManager.Localize(key),
                    code)
                : errorMsg;
            return (key, code, errorMsg);
        }
    }
}
