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
        public Text text;

        private void Awake()
        {
            btnLogin.SetActive(false);
            nameField.gameObject.SetActive(false);
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
            var key = $"private_key_{index}";
            string k = PlayerPrefs.GetString(key, "");
            PlayerPrefs.DeleteKey(key);
            Debug.Log($"Delete {key}: {k}");
            string datPath = System.IO.Path.Combine(Application.persistentDataPath, $"avatar_{index}.dat");
            if (System.IO.File.Exists(datPath))
                System.IO.File.Delete(datPath);
            PlayerPrefs.Save();
        }
    }
}
