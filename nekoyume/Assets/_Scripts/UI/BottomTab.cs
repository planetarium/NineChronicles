using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BottomTab : MonoBehaviour
    {
        public Button goToMainButton;
        public Button inventoryButton;
        public Button QuestButton;
        public Button infoAndEquipButton;
        public Button mailButton;
        public Button dictionaryButton;

        private enum ScreenState
        {
            Main,
            Dungeon
        }

        void Start()
        {

        }


    }
}
