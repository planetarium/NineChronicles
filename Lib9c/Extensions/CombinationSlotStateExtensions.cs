using System;
using Nekoyume.Action;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;

namespace Nekoyume.Extensions
{
    public static class CombinationSlotStateExtensions
    {
        public static bool TryGetResultId(this CombinationSlotState state, out Guid resultId)
        {
            if (state?.Result is null)
            {
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
