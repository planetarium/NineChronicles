using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume
{
    public class Neko : UI.Widget
    {
        static private Neko Instance;

        public Text log;

        private Transform _modal;
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

        public override void Show()
        {
            _modal.gameObject.SetActive(true);
        }

        public override void Close()
        {
            _modal.gameObject.SetActive(false);
        }

        public override bool IsActive()
        {
            return _modal.gameObject.activeSelf;
        }

        public void HandleClick(GameObject sender)
        {
#if DEBUG
            Invoke(sender.name, 0.0f);
#endif
        }

        public void CleanUpAvatar()
        {
            string[] keys = new []
            {
                "private_key",
                "avatar",
                "last_block_id"
            };
            foreach (var key in keys)
            {
                string k = PlayerPrefs.GetString(key, "");
                PlayerPrefs.DeleteKey(key);
                Neko.Log($"Delete {key}: {k}");
            }
            PlayerPrefs.Save();

        }
    }
}
