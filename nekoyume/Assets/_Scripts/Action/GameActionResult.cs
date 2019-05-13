using System;
using System.Collections.Generic;

namespace Nekoyume.Action
{
    [Serializable]
    public class GameActionResult
    {
        [Serializable]
        public struct ErrorCode
        {
            public const int Success = 0;
            public const int Fail = -1;
            public const int KeyNotFoundInTable = -2;

            #region Sell

            public const int SellItemNotFoundInInventory = -100;

            #endregion
        }
        
        public int errorCode;
    }
}
