using System;
using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class UIOnClickSound : MonoBehaviour
    {
        public enum SfxType {
            Click,
            Cancel,
        }

        [SerializeField]
        public SfxType type;

        private bool isDone = false;
        public void Awake()
        {
            PutSoundOnButton();
            PutSoundOnToggle();
            PutSoundOnDropdown();
        }

        private void PutSoundOnButton()
        {
            if (isDone)
            {
                return;
            }

            var button = GetComponent<Button>();
            if (button)
            {
                isDone = true;
                button.onClick.AddListener(Play);
            }
        }

        private void PutSoundOnToggle()
        {
            if (isDone)
            {
                return;
            }

            var toggle = GetComponent<Toggle>();
            if (toggle)
            {
                toggle.onValueChanged.AddListener(isOn =>
                {
                    isDone = true;
                    if (isOn)
                    {
                        Play();
                    }
                });
            }
        }

        private void PutSoundOnDropdown()
        {
            if (isDone)
            {
                return;
            }

            var dropdown = GetComponent<Dropdown>();
            if (dropdown)
            {
                isDone = true;
                dropdown.onValueChanged.AddListener(i => Play());
            }
        }

        public void Play()
        {
            switch (type)
            {
                case SfxType.Click:
                    AudioController.instance.PlaySfx(AudioController.SfxCode.Click);
                    break;
                case SfxType.Cancel:
                    AudioController.instance.PlaySfx(AudioController.SfxCode.Cancel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
