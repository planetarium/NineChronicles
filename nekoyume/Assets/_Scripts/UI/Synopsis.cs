using System;
using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Synopsis : Widget
    {
        public TextMeshProUGUI[] texts;

        protected override void Awake()
        {
            base.Awake();

            for (var i = 0; i < texts.Length; i++)
            {
                texts[i].text = LocalizationManager.Localize($"UI_SYNOPSIS_{i}");
            }

            CloseWidget = End;
            SubmitWidget = End;
        }

        public override void Show()
        {
            base.Show();
            AudioController.instance.PlayMusic(AudioController.MusicCode.Prologue);
        }

        public void End()
        {
            Game.Event.OnNestEnter.Invoke();
            Find<Login>().Show();
            Close();
        }
    }
}
