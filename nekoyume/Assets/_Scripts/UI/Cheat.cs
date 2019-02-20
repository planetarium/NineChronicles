using System.Linq;
using System.Text;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class Cheat : Widget
    {
        private static Cheat Instance;

        public Text log;
        public Model.BattleResult.Result result;

        private Transform _modal;
        private float _updateTime = 0.0f;
        private StringBuilder _logString = new StringBuilder();

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

        private void LevelUp()
        {
            GameObject enemyObj = GameObject.Find("Enemy");
            if (enemyObj == null)
            {
                Log("Need Enemy.");
                return;
            }
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                var player = playerObj.GetComponent<Player>();
                player.Level += 1;
                Log($"Level Up to {player.Level}");
            }
            var enemy = enemyObj.GetComponent<Enemy>();
            Game.Event.OnEnemyDead.Invoke(enemy);
        }

        private void SpeedUp()
        {
            Time.timeScale = 2.0f;
        }

        private void DummyBattle()
        {
            Find<BattleResult>()?.Close();
            GameObject stage = GameObject.Find("Stage");
            var simulator = new Simulator(0, ActionManager.Instance.Avatar);
            simulator.Simulate();
            var battleResult = simulator.log.events.OfType<Model.BattleResult>().First();
            battleResult.result = result;
            stage.GetComponent<Stage>().Play(simulator.log);
        }
    }
}
