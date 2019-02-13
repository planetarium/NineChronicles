using System;
using System.Collections;
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
        public GameObject deletePopUp;
        public Text text;
        private GameObject _player;
        private int _selectedIndex;

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
                    slotText.text = $"LV.{avatar.Level} {avatar.Name}";
                    var button = slot.transform.Find("Character").Find("Button");
                    button.gameObject.SetActive(false);
                }
                catch (ArgumentOutOfRangeException)
                {
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
            StartCoroutine(SlotClickAsync(index));
        }

        private IEnumerator SlotClickAsync(int index)
        {
            var slot = slots[index];
            _selectedIndex = index;
            ActionManager.Instance.Init(index);
            ActionManager.Instance.StartSync();
            modal.SetActive(false);
            LoadStatus();
            btnLogin.SetActive(true);
            nameField.gameObject.SetActive(true);
            yield break;
        }

        public void DeleteSlot()
        {
            var prefsKey = string.Format(ActionManager.PrivateKeyFormat, _selectedIndex);
            string privateKey = PlayerPrefs.GetString(prefsKey, "");
            PlayerPrefs.DeleteKey(prefsKey);
            Debug.Log($"Delete {prefsKey}: {privateKey}");
            var fileName = string.Format(ActionManager.AvatarFileFormat, _selectedIndex);
            string datPath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            if (System.IO.File.Exists(datPath))
                System.IO.File.Delete(datPath);
            PlayerPrefs.Save();
        }

        public void DeleteClick()
        {
            deletePopUp.SetActive(true);
        }

        public void LoadStatus()
        {
            try
            {
                var avatar = ActionManager.Instance.Avatars[_selectedIndex];

            }
            catch (ArgumentOutOfRangeException)
            {
            }

        }
    }
}
