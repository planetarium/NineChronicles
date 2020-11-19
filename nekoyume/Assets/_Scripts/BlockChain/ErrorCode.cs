using System;
using Libplanet.Action;
using Libplanet.Tx;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.BlockChain
{
    public class ErrorCode
    {
        public static (string, string, string) GetErrorCode(Exception exc)
        {
            var key = "ERROR_UNKNOWN";
            var code = "99";
            var errorMsg = String.Empty;
            switch (exc)
            {
                case TimeoutException _:
                    key = "ERROR_NETWORK";
                    code = "00";
                    break;
                case RequiredBlockIndexException _:
                    key = "ERROR_REQUIRE_BLOCK";
                    code = "01";
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
                    code = "14";
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
                    key = "ERROR_NETWORK";
                    errorMsg = "Action timeout occurred.";
                    if (Game.Game.instance.Agent.Transactions.TryGetValue(ate.ActionId, out TxId txid)
                        && Game.Game.instance.Agent.IsTransactionStaged(txid))
                    {
                        errorMsg += $" Transaction for action is still staged. (txid: {txid})";
                        code = "26";
                    }
                    else
                    {
                        code = "27";
                    }

                    errorMsg += $"\nError Code: {code}";
                    break;
            }

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
