using System;
using System.Collections;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Login : Widget
    {
        public bool ready = false;
        public GameObject[] slots;

        protected override void Awake()
        {
            base.Awake();

            Show();
        }

        public void SlotClick(int index)
        {
            if (!ready)
                return;

            Game.Event.OnLoginDetail.Invoke(index);
            gameObject.SetActive(false);
            AudioController.PlayClick();
        }

        public override void Show()
        {
            base.Show();

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var slotText = slot.GetComponentInChildren<Text>();
                var button = slot.transform.Find("Character").Find("Button");
                button.gameObject.SetActive(true);
                try
                {
                    var avatar = ActionManager.instance.Avatars[i];
                    slotText.text = $"LV.{avatar.Level} {avatar.Name}";
                    button.gameObject.SetActive(false);
                }
                catch (Exception e)
                {
                    if (e is ArgumentOutOfRangeException || e is NullReferenceException)
                    {
                        slotText.text = "캐릭터를 생성하세요.";

                        var tween = slot.GetComponentInChildren<UI.Tween.DOTweenImageAlpha>();
                        if (tween)
                            tween.gameObject.SetActive(false);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            StartCoroutine(CoPlayMusic());
        }

        private IEnumerator CoPlayMusic()
        {
            while (AudioController.instance.state != AudioController.State.Idle)
            {
                yield return null;
            }
            
            AudioController.instance.PlayMusic(AudioController.MusicCode.SelectCharacter);
        }
    }
}
