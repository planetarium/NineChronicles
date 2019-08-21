using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BottomTab : Widget
    {
        [Flags]
        public enum ButtonHideFlag
        {
            None =          1 << 1,
            Main =          1 << 2,
            Inventory =     1 << 3,
            Quest =         1 << 4,
            InfoAndEquip =  1 << 5,
            Mail =          1 << 6,
            Dictionary =    1 << 7,
        }
        
        public Button goToMainButton;
        public Button inventoryButton;
        public Button QuestButton;
        public Button infoAndEquipButton;
        public Button mailButton;
        public Button dictionaryButton;

        private Dictionary<ButtonHideFlag, Button> _buttonDict;

        protected override void Awake()
        {
            base.Awake();

            _buttonDict = new Dictionary<ButtonHideFlag, Button>()
            {
                { ButtonHideFlag.Main,         MapButton(goToMainButton, GoToMain) },
                { ButtonHideFlag.Inventory,    MapButton(inventoryButton, Find<Status>().ToggleInventory) },
                { ButtonHideFlag.Quest,        MapButton(QuestButton, Find<Status>().ToggleQuest) },
                { ButtonHideFlag.InfoAndEquip, MapButton(infoAndEquipButton, Find<Status>().ToggleStatus) },
                { ButtonHideFlag.Mail,         MapButton(mailButton, null) },
                { ButtonHideFlag.Dictionary,   MapButton(dictionaryButton, null)},
            };
        }

        public void Show(ButtonHideFlag flag)
        {
            base.Show();
            UpdateButton(flag);
        }

        public void UpdateButton(ButtonHideFlag flag)
        {
            foreach (var pair in _buttonDict)
            {
                bool condition = !flag.HasFlag(pair.Key);
                _buttonDict[pair.Key].gameObject.SetActive(condition);
            }
        }

        public override void Close()
        {
            base.Close();

        }

        public void FadeIn(float duration)
        {
            if (Animator)
            {
                Animator.enabled = true;
                Animator.Play("FadeIn");
                Animator.speed = 1.0f / ((duration != 0) ? duration : 1f);
            }
        }

        private Button MapButton(Button button, UnityAction action)
        {
            if (action == null) return button;
            button.onClick.AddListener(action);
            return button;
        }

        private void GoToMain()
        {
            Find<QuestPreparation>()?.BackClick();

            Find<Menu>()?.Show();
        }
    }
}
