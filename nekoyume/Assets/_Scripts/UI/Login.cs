using System;
using Nekoyume.Action;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Login : Widget
    {
        public bool ready = false;
        public GameObject[] slots;

        private void Awake()
        {
            Show();
        }

        public void SlotClick(int index)
        {
            if (!ready)
                return;

            Game.Event.OnLoginDetail.Invoke(index);
            gameObject.SetActive(false);
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
                    var avatar = ActionManager.Instance.Avatars[i];
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
        }
    }
}
