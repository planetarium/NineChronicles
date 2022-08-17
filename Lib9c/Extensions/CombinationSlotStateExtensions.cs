using System;
using Nekoyume.Action;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;

namespace Nekoyume.Extensions
{
    public static class CombinationSlotStateExtensions
    {
        public static void ValidateFromAction(
            this CombinationSlotState slotState,
            long blockIndex,
            AvatarState avatarState,
            int slotIndex,
            string actionTypeText,
            string addressesHex)
        {
            if (slotState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the slot state is failed to load: # {slotIndex}");
            }

            if (!slotState.Validate(avatarState, blockIndex))
            {
                throw new CombinationSlotUnlockException(
                    $"{addressesHex}Aborted as the slot state is invalid: {slotState} @ {slotIndex}");
            }
        }
        
        public static bool TryGetResultId(this CombinationSlotState state, out Guid resultId)
        {
            if (state?.Result is null)
            {
                resultId = default(Guid);
                return false;
            }

            switch (state.Result)
            {
                case Buy7.BuyerResult r:
                    resultId = r.id;
                    break;
                case Buy7.SellerResult r:
                    resultId = r.id;
                    break;
                case DailyReward2.DailyRewardResult r:
                    resultId = r.id;
                    break;
                case ItemEnhancement.ResultModel r:
                    resultId = r.id;
                    break;
                case ItemEnhancement7.ResultModel r:
                    resultId = r.id;
                    break;
                case ItemEnhancement9.ResultModel r:
                    resultId = r.id;
                    break;
                case ItemEnhancement10.ResultModel r:
                    resultId = r.id;
                    break;
                case MonsterCollectionResult r:
                    resultId = r.id;
                    break;
                case CombinationConsumable5.ResultModel r:
                    resultId = r.id;
                    break;
                case RapidCombination5.ResultModel r:
                    resultId = r.id;
                    break;
                case SellCancellation.Result r:
                    resultId = r.id;
                    break;
                case SellCancellation7.Result r:
                    resultId = r.id;
                    break;
                case SellCancellation8.Result r:
                    resultId = r.id;
                    break;
                default:
                    resultId = default(Guid);
                    return false;
            }

            return true;
        }

        [Obsolete("Use TryGetResultId instead")]
        public static bool TryGetResultIdV1(this CombinationSlotState state, out Guid resultId)
        {
            if (state?.Result is null)
            {
                resultId = default(Guid);
                return false;
            }

            switch (state.Result)
            {
                case Buy7.BuyerResult r:
                    resultId = r.id;
                    break;
                case Buy7.SellerResult r:
                    resultId = r.id;
                    break;
                case DailyReward2.DailyRewardResult r:
                    resultId = r.id;
                    break;
                case ItemEnhancement.ResultModel r:
                    resultId = r.id;
                    break;
                case ItemEnhancement7.ResultModel r:
                    resultId = r.id;
                    break;
                case MonsterCollectionResult r:
                    resultId = r.id;
                    break;
                case CombinationConsumable5.ResultModel r:
                    resultId = r.id;
                    break;
                case RapidCombination5.ResultModel r:
                    resultId = r.id;
                    break;
                case SellCancellation.Result r:
                    resultId = r.id;
                    break;
                case SellCancellation7.Result r:
                    resultId = r.id;
                    break;
                case SellCancellation8.Result r:
                    resultId = r.id;
                    break;
                default:
                    resultId = default(Guid);
                    return false;
            }

            return true;
        }

        public static bool TryGetMail(
            this CombinationSlotState state,
            long blockIndex,
            long requiredBlockIndex,
            out CombinationMail combinationMail,
            out ItemEnhanceMail itemEnhanceMail)
        {
            combinationMail = null;
            itemEnhanceMail = null;

            if (!state.TryGetResultId(out var resultId))
            {
                return false;
            }

            switch (state.Result)
            {
                case ItemEnhancement.ResultModel r:
                    itemEnhanceMail = new ItemEnhanceMail(
                        r,
                        blockIndex,
                        resultId,
                        requiredBlockIndex);
                    return true;
                case ItemEnhancement7.ResultModel r:
                    itemEnhanceMail = new ItemEnhanceMail(
                        r,
                        blockIndex,
                        resultId,
                        requiredBlockIndex);
                    return true;
                case ItemEnhancement9.ResultModel r:
                    itemEnhanceMail = new ItemEnhanceMail(
                        r,
                        blockIndex,
                        resultId,
                        requiredBlockIndex);
                    return true;
                case ItemEnhancement10.ResultModel r:
                    itemEnhanceMail = new ItemEnhanceMail(
                        r,
                        blockIndex,
                        resultId,
                        requiredBlockIndex);
                    return true;
                case CombinationConsumable5.ResultModel r:
                    combinationMail = new CombinationMail(
                        r,
                        blockIndex,
                        resultId,
                        requiredBlockIndex);
                    return true;
                default:
                    return false;
            }
        }

        [Obsolete("Use TryGetMail instead")]
        public static bool TryGetMailV1(
            this CombinationSlotState state,
            long blockIndex,
            long requiredBlockIndex,
            out CombinationMail combinationMail,
            out ItemEnhanceMail itemEnhanceMail)
        {
            combinationMail = null;
            itemEnhanceMail = null;

            if (!state.TryGetResultIdV1(out var resultId))
            {
                return false;
            }

            switch (state.Result)
            {
                case ItemEnhancement.ResultModel r:
                    itemEnhanceMail = new ItemEnhanceMail(
                        r,
                        blockIndex,
                        resultId,
                        requiredBlockIndex);
                    return true;
                case ItemEnhancement7.ResultModel r:
                    itemEnhanceMail = new ItemEnhanceMail(
                        r,
                        blockIndex,
                        resultId,
                        requiredBlockIndex);
                    return true;
                case CombinationConsumable5.ResultModel r:
                    combinationMail = new CombinationMail(
                        r,
                        blockIndex,
                        resultId,
                        requiredBlockIndex);
                    return true;
                default:
                    return false;
            }
        }
    }
}
