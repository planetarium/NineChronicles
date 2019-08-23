using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BottomMenu : MonoBehaviour
    {
        public Button goToMainButton;
        public Button inventoryButton;
        public Button questButton;
        public Button infoAndEquipButton;
        public Button mailButton;
        public Button dictionaryButton;
        public Animator animator;

        private List<Widget> _widgetsForClose;
        private bool _isInitialized;

        public void Show()
        {
            if (!_isInitialized)
                Init();

            animator = GetComponent<Animator>();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            if (_isInitialized)
            {
                foreach (var widget in _widgetsForClose)
                {
                    widget.Close();
                }
            }
            gameObject.SetActive(false);
        }

        private void MapButton(Button button, UnityAction action)
        {
            if (!button.gameObject.activeSelf) return;
            button.onClick.AddListener(action);
        }

        private void GoToMain()
        {
            Widget.Find<QuestPreparation>()?.BackClick();

            Widget.Find<Menu>()?.Show();
        }

        private void Init()
        {
            _widgetsForClose = new List<Widget>
            {
                Widget.Find<Inventory>(),
                Widget.Find<Quest>(),
                Widget.Find<StatusDetail>(),
            };

            var status = Widget.Find<Status>();
            MapButton(goToMainButton, GoToMain);
            MapButton(inventoryButton, status.ToggleInventory);
            MapButton(questButton, status.ToggleQuest);
            MapButton(infoAndEquipButton, status.ToggleStatus);

            _isInitialized = true;
        }
    }
}
