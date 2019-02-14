using System;
using Nekoyume.Action;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Login : Widget
    {
        public GameObject[] slots;

        private void Awake()
        {
            Init();
        }

        public void SlotClick(int index)
        {
            Game.Event.OnLoginDetail.Invoke(index);
            gameObject.SetActive(false);
        }

        public void Init()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var slotText = slot.GetComponentInChildren<Text>();
                var button = slot.transform.Find("Character").Find("Button");
                try
                {
                    var avatar = ActionManager.Instance.Avatars[i];
                    slotText.text = $"LV.{avatar.Level} {avatar.Name}";
                    button.gameObject.SetActive(false);
                }
                catch (ArgumentOutOfRangeException)
                {
                    slotText.text = "캐릭터를 생성하세요.";
                    button.gameObject.SetActive(true);
                }
            }
        }
    }
}
