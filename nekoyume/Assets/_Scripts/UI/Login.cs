using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Login : Widget
    {
        public GameObject btnLogin;
        public GameObject modal;
        public InputField nameField;
        public GameObject[] slots;
        public Text text;

        private void Awake()
        {
            btnLogin.SetActive(false);
            nameField.gameObject.SetActive(false);
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var bg = slot.GetComponent<Image>();
                var slotText = slot.GetComponentInChildren<Text>();
                try
                {
                    var avatar = ActionManager.Instance.Avatars[i];
                    slotText.text = $"{avatar.Name} LV.{avatar.Level}";
                }
                catch (ArgumentOutOfRangeException)
                {
                    bg.sprite = null;
                    var color = bg.color;
                    color.a = 0.1f;
                    bg.color = color;
                    var button = slot.transform.Find("Button");
                    button.gameObject.SetActive(false);
                }
            }
        }

        public void LoginClick()
        {
            btnLogin.SetActive(false);
            nameField.gameObject.SetActive(false);
            text.text = "Connecting...";
            var nickName = nameField.text;
            ActionManager.Instance.CreateNovice(nickName);
        }

        public void SlotClick(int index)
        {
            ActionManager.Instance.Init(index);
            ActionManager.Instance.StartSync();
            modal.SetActive(false);
            btnLogin.SetActive(true);
            nameField.gameObject.SetActive(true);

        }
        public void DeleteSlot(int index)
        {
            var prefsKey = string.Format(ActionManager.PrivateKeyFormat, index);
            string privateKey = PlayerPrefs.GetString(prefsKey, "");
            PlayerPrefs.DeleteKey(prefsKey);
            Debug.Log($"Delete {prefsKey}: {privateKey}");
            var fileName = string.Format(ActionManager.AvatarFileFormat, index);
            string datPath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            if (System.IO.File.Exists(datPath))
                System.IO.File.Delete(datPath);
            PlayerPrefs.Save();
            slots[index].GetComponentInChildren<Text>().text = "New";
        }
    }
}
