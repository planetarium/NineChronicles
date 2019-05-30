using System;
using System.Collections.Generic;

namespace Nekoyume.Action
{
    public abstract partial class GameAction
    {
        [Serializable]
        public struct ErrorCode
        {
            public const int Success = 0;
            /// <summary>
            /// 액션 자체가 실패함.
            /// </summary>
            public const int Fail = -1;
            /// <summary>
            /// 예상하지 못한 경우가 액션 실행부에서 발생함.
            /// </summary>
            public const int UnexpectedCaseInActionExecute = -2;
            /// <summary>
            /// 테이블 데이터에서 키를 찾지 못함.
            /// </summary>
            public const int KeyNotFoundInTable = -3;
            
            public const int AgentNotFound = -10;
            public const int AvatarNotFound = -20;

            #region CreateAvatar

            public const int CreateAvatarAlreadyExistKeyAvatarAddress = -100;
            public const int CreateAvatarAlreadyExistAvatarAddress = -101;

            #endregion

            #region DeleteAvatar

            public const int DeleteAvatarNotFoundKeyInAvatarAddresses = -100;
            public const int DeleteAvatarNotEqualsAvatarAddressToValueInAvatarAddresses = -100;

            #endregion

            #region Combination

            public const int CombinationNotFoundMaterials = -100;
            public const int CombinationNoResultItem = -101;

            #endregion

            #region Sell

            public const int SellItemNotFoundInInventory = -100;
            public const int SellItemCountNotEnoughInInventory = -101;

            #endregion

            #region Buy

            public const int BuyBuyerAgentNotFound = -100;
            public const int BuyBuyerAvatarNotFound = -101;
            public const int BuySellerAgentNotFound = -102;
            public const int BuyGoldNotEnough = -103;
            public const int BuySoldOut = -104;

            #endregion
        }
    }
}
