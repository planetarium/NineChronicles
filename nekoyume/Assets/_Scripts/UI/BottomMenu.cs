using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class BottomMenu : MonoBehaviour
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
            All =           int.MaxValue
        }

        public Button goToMainButton;
        public Button inventoryButton;
        public Button questButton;
        public Button infoAndEquipButton;
        public Button mailButton;
        public Button dictionaryButton;
        public Animator animator;

        private Dictionary<ButtonHideFlag, Button> _buttonDict;
        private List<Widget> _widgetsForClose;

        public void Show(ButtonHideFlag flag)
        {
            Init();
            animator = GetComponent<Animator>();
            gameObject.SetActive(true);

            if (flag.HasFlag(ButtonHideFlag.None)) return;
            foreach (var pair in _buttonDict)
            {
                bool condition = !flag.HasFlag(pair.Key);
                _buttonDict[pair.Key].gameObject.SetActive(condition);
            }
        }

        public void Close()
        {
            foreach(var widget in _widgetsForClose)
            {
                widget.Close();
            }
            _widgetsForClose.Clear();
            gameObject.SetActive(false);
        }

        public void FadeInColor(float duration = 1f)
        {
            if (animator)
            {
                animator.enabled = true;
                animator.Play("FadeInColor");
                animator.speed = 1.0f / ((duration != 0) ? duration : 1f);
            }
        }

        public void FadeInAlpha(float duration = 1f)
        {
            if (animator)
            {
                animator.enabled = true;
                animator.Play("FadeInAlpha");
                animator.speed = 1.0f / ((duration != 0) ? duration : 1f);
            }
        }

        private void MapButton(Button button, UnityAction action)
        {
            button.onClick.RemoveAllListeners();
            if (action == null) return;
            button.onClick.AddListener(action);
        }

        private void GoToMain()
        {
            Widget.Find<QuestPreparation>()?.BackClick();

            Widget.Find<Menu>()?.Show();
        }

        private void Init()
        {
            if (ReferenceEquals(_buttonDict, null))
            {
                _buttonDict = new Dictionary<ButtonHideFlag, Button>()
                {
                    { ButtonHideFlag.Main, goToMainButton },
                    { ButtonHideFlag.Inventory, inventoryButton },
                    { ButtonHideFlag.Quest, questButton },
                    { ButtonHideFlag.InfoAndEquip, infoAndEquipButton },
                    { ButtonHideFlag.Mail, mailButton },
                    { ButtonHideFlag.Dictionary, dictionaryButton },
                };
            }
            if (ReferenceEquals(_widgetsForClose, null))
            {
                _widgetsForClose = new List<Widget>();
                _widgetsForClose.Add(Widget.Find<Inventory>());
                _widgetsForClose.Add(Widget.Find<Quest>());
                _widgetsForClose.Add(Widget.Find<StatusDetail>());
            }

            MapButton(_buttonDict[ButtonHideFlag.Main], GoToMain);
            MapButton(_buttonDict[ButtonHideFlag.Inventory], Widget.Find<Status>().ToggleInventory);
            MapButton(_buttonDict[ButtonHideFlag.Quest], Widget.Find<Status>().ToggleQuest);
            MapButton(_buttonDict[ButtonHideFlag.InfoAndEquip], Widget.Find<Status>().ToggleStatus);
            MapButton(_buttonDict[ButtonHideFlag.Mail], null);
            MapButton(_buttonDict[ButtonHideFlag.Dictionary], null);
        }
    }
}
