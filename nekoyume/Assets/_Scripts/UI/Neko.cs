using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume
{
    public class Neko : UI.Widget
    {
        static private Neko Instance;

        public Text log;

        private Transform _modal;
        private int _clickCount = 0;
        private float _updateTime = 0.0f;

        static void Log(string text)
        {
            Instance.log.text += $"> {text}\n";
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _modal = transform.Find("Modal");
            _modal.gameObject.SetActive(false);
#if DEBUG
#else
            Transform btn = transform.Find("Btn");
            btn.gameObject.SetActive(false);
#endif
        }

        private void Update()
        {
            _updateTime += Time.deltaTime;
        }

        override public void Show()
        {
            _modal.gameObject.SetActive(true);
        }

        override public void Close()
        {
            _modal.gameObject.SetActive(false);
        }

        override public bool IsActive()
        {
            return _modal.gameObject.activeSelf;
        }

        public void HandleClick(GameObject sender)
        {
#if DEBUG
            Invoke(sender.name, 0.0f);
#endif
        }

        public void DeletePrivateKey()
        {
            const string prefsKey = "private_key";
            string privateKey = PlayerPrefs.GetString(prefsKey, "");
            PlayerPrefs.SetString(prefsKey, "");
            PlayerPrefs.DeleteKey(prefsKey);
            Neko.Log($"DeletePrivateKey: {privateKey}");
        }
    }
}
