using System;

namespace Nekoyume.Game.LiveAsset
{
    [Serializable]
    public class GameConfig
    {
        public int SecondsPerBlock { get; set; }

        #region action

        public const int CombinationEquipmentAction = 3;
        public const int CombinationConsumableAction = 6;
        public const int ItemEnhancementAction = 9;
        public const int ActionsInShop = 17;
        public const int ActionsInRankingBoard = 25;
        public const int ActionsInMimisbrunnr = 100;
        public const int ActionsInRaid = 50;

        #endregion

        #region ui

        public const int UIMainMenuStage = 0;
        public const int UIMainMenuCombination = CombinationEquipmentAction;
        public const int UIMainMenuShop = ActionsInShop;
        public const int UIMainMenuRankingBoard = ActionsInRankingBoard;
        public const int UIMainMenuMimisbrunnr = ActionsInMimisbrunnr;

        public const int UIBottomMenuInBattle = 1;
        public const int UIBottomMenuCharacter = 1;
        public const int UIBottomMenuMail = CombinationEquipmentAction;
        public const int UIBottomMenuChat = 7;
        public const int UIBottomMenuQuest = 1;
        public const int UIBottomMenuMimisbrunnr = ActionsInMimisbrunnr;
        public const int UIBottomMenuRanking = 3;

        #endregion
    }
}
