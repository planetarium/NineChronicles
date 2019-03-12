using System.Text;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Model;
using Nekoyume.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class Cheat : Widget
    {
        private static Cheat Instance;

        public Text log;
        public Button BtnOpen;

        private Transform _modal;
        private float _updateTime = 0.0f;
        private StringBuilder _logString = new StringBuilder();


        private class DebugRandom : IRandom
        {
            public int Next()
            {
                return new System.Random().Next();
            }

            public int Next(int maxValue)
            {
                return new System.Random().Next(maxValue);
            }

            public int Next(int minValue, int maxValue)
            {
                return new System.Random().Next(minValue, maxValue);
            }

            public void NextBytes(byte[] buffer)
            {
                new System.Random().NextBytes(buffer);
            }

            public double NextDouble()
            {
                return new System.Random().NextDouble();
            }
        }
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
            BtnOpen.gameObject.SetActive(false);
            base.Show();
        }

        public override void Close()
        {
            _modal.gameObject.SetActive(false);
            BtnOpen.gameObject.SetActive(true);
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
                var player = playerObj.GetComponent<Game.Character.Player>();
                player.Level += 1;
                Log($"Level Up to {player.Level}");
            }
            var enemy = enemyObj.GetComponent<Enemy>();
            Game.Event.OnEnemyDead.Invoke(enemy);
        }

        private void SpeedUp()
        {
            Time.timeScale = 2.0f;
            Log($"Speed Up to {Time.timeScale}");
        }

        private void DummyBattleWin()
        {
            Find<BattleResult>()?.Close();
            GameObject stage = GameObject.Find("Stage");
            var simulator = new Simulator(new DebugRandom(), ActionManager.Instance.Avatar);
            simulator.Simulate();
            simulator.Log.result = BattleLog.Result.Win;
            stage.GetComponent<Stage>().Play(simulator.Log);
            Close();
        }

        private void DummyBattleLose()
        {
            Find<BattleResult>()?.Close();
            GameObject stage = GameObject.Find("Stage");
            var simulator = new Simulator(new DebugRandom(), ActionManager.Instance.Avatar);
            simulator.Simulate();
            simulator.Log.result = BattleLog.Result.Lose;
            stage.GetComponent<Stage>().Play(simulator.Log);
            Close();
        }
    }
}
