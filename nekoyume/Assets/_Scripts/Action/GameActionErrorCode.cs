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
            public const int Fail = -1;
            public const int UnexpectedInternalAction = -2;
            public const int KeyNotFoundInTable = -3;

            #region CreateNovice

            public const int CreateAvatarAlreadyExistAvatarAddress = -100;

            #endregion

            #region Sell

            public const int SellItemNotFoundInInventory = -100;
            public const int SellItemCountNotEnoughInInventory = -101;

            #endregion

            #region Buy

            public const int BuyGoldNotEnough = -100;
            public const int BuySoldOut = -101;

            #endregion
        }
    }
}
