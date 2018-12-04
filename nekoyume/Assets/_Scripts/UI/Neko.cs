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
        private System.Text.StringBuilder _logString = new System.Text.StringBuilder();

        static void Log(string text)
        {
            Instance._logString.Insert(0, $"> {text}\n");
            Instance.log.text += Instance._logString.ToString();
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
            string datPath = System.IO.Path.Combine(Application.persistentDataPath, "avatar.dat");
            if (System.IO.File.Exists(datPath))
                System.IO.File.Delete(datPath);
            PlayerPrefs.Save();
        }

        private void LevelUp()
        {
            GameObject enemyObj = GameObject.Find("Enemy");
            if (enemyObj == null)
            {
                Neko.Log("Need Enemy.");
                return;
            }
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                var player = playerObj.GetComponent<Game.Character.Player>();
                player.Level += 1;
                Neko.Log($"Level Up to {player.Level}");
            }
            var enemy = enemyObj.GetComponent<Game.Character.Enemy>();
            Game.Event.OnEnemyDead.Invoke(enemy);
        }

        private void SpeedUp()
        {
            Time.timeScale = 2.0f;
        }
    }
}
