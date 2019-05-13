using System;
using System.Collections.Generic;

namespace Nekoyume.Action
{
    public class GameActionResult
    {
        public struct ErrorCode
        {
            public const int Success = 0;
            public const int Fail = -1;

            #region Sell

            public const int SellItemNotFound = -2;

            #endregion
            
        }
        
        public int errorCode;
    }
}
