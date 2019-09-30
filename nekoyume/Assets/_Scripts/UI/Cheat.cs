using System.Collections.Generic;
using System.Linq;
using System.Text;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.TableData;
using Nekoyume.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume
{
    public class Cheat : ScreenWidget
    {
        private static Cheat Instance;

        public TextMeshProUGUI Logs;
        public Transform Peers;
        public Transform StagedTxs;
        public Button BtnOpen;
        public Button buttonBase;
        public ScrollRect list;
        public ScrollRect skillList;
        public HorizontalLayoutGroup skillPanel;

        private Transform _modal;
        private float _updateTime = 0.0f;
        private StringBuilder _logString = new StringBuilder();
        private BattleLog.Result _result;
        private int[,] _stageRange;
        private Game.Skill[] _skills;
        private Game.Skill _selectedSkill;

        public class DebugRandom : IRandom
        {
            private readonly System.Random _random = new System.Random();
            public int Next()
            {
                return _random.Next();
            }

            public int Next(int maxValue)
            {
                return _random.Next(maxValue);
            }

            public int Next(int minValue, int maxValue)
            {
                return _random.Next(minValue, maxValue);
            }

            public void NextBytes(byte[] buffer)
            {
                _random.NextBytes(buffer);
            }

            public double NextDouble()
            {
                return _random.NextDouble();
            }
        }

        public static void Display(string target, string text)
        {
            switch (target)
            {
                case "Logs":
                    Instance.Logs.text = text;
                    break;
                case "Peers":
                    Instance.Peers.Find("TextRect/Text").GetComponent<TextMeshProUGUI>().text = text;
                    Instance.Refresh(Instance.Peers);
                    break;
                case "StagedTxs":
                    Instance.StagedTxs.Find("TextRect/Text").GetComponent<TextMeshProUGUI>().text = text;
                    Instance.Refresh(Instance.StagedTxs);
                    break;
            }
        }
        
        public static void Log(string text)
        {
            Instance._logString.Insert(0, $"> {text}\n");
            Instance.Logs.text += Instance._logString.ToString();
        }

        private void Refresh(Transform target)
        {
            // 마스크에 짤려서 사이즈 조정
            var delta = target.Find("TextRect/Text").GetComponent<TextMeshProUGUI>().preferredHeight -
                        target.GetComponent<RectTransform>().rect.height;
            target.Find("TextRect/Text").GetComponent<RectTransform>().sizeDelta =
                new Vector2(0, delta < 0 ? 0 : delta);
            
            // 스크롤바 조정
            if (delta < 0)
            {
                target.Find("Scrollbar").gameObject.SetActive(false);
                ScrollBarHandler(target, 0);
            }
            else
            {
                target.Find("Scrollbar").gameObject.SetActive(true);
                target.Find("Scrollbar").GetComponent<Scrollbar>().size =
                    target.GetComponent<RectTransform>().rect.height /
                    target.Find("TextRect/Text").GetComponent<TextMeshProUGUI>().preferredHeight;
            }
        }

        private void ScrollBarHandler(Transform target, float location)
        {
            var delta = target.Find("TextRect/Text").GetComponent<TextMeshProUGUI>().preferredHeight -
                        target.GetComponent<RectTransform>().rect.height;
            target.Find("TextRect/Text").GetComponent<RectTransform>().anchoredPosition =
                delta > 0 ?
                    new Vector2(0, delta * (location - 1)) :
                    new Vector2(0, 0);
        }

        protected override void Awake()
        {
            base.Awake();

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

            Peers.Find("Scrollbar")
                .GetComponent<Scrollbar>()
                .onValueChanged
                .AddListener((location) => ScrollBarHandler(Peers, location));
            StagedTxs.Find("Scrollbar")
                .GetComponent<Scrollbar>()
                .onValueChanged
                .AddListener((location) => ScrollBarHandler(StagedTxs, location));
            Refresh(Peers);
            Refresh(StagedTxs);
            ScrollBarHandler(Peers, 0);
            ScrollBarHandler(StagedTxs, 0);

            BtnOpen.gameObject.SetActive(false);
            foreach (var i in Enumerable.Range(1, Game.Game.instance.TableSheets.StageSheet.Count))
            {
                Button newButton = Instantiate(buttonBase, list.content);
                newButton.GetComponentInChildren<Text>().text = i.ToString();
                newButton.onClick.AddListener(() => DummyBattle(i));
                newButton.gameObject.SetActive(true);
            }

            var skills = new List<Game.Skill>();
            foreach (var skillRow in Game.Game.instance.TableSheets.SkillSheet)
            {
                var skill = SkillFactory.Get(skillRow, 50, 1m);
                skills.Add(skill);
                Button newButton = Instantiate(buttonBase, skillList.content);
                newButton.GetComponentInChildren<Text>().text = $"{skillRow.GetLocalizedName()}_{skillRow.ElementalType}";
                newButton.onClick.AddListener(() => SelectSkill(skill));
                newButton.gameObject.SetActive(true);
            }

            _skills = skills.ToArray();

            base.Show();
        }

        public override void Close()
        {
            Peers.Find("Scrollbar").GetComponent<Scrollbar>().onValueChanged.RemoveAllListeners();
            StagedTxs.Find("Scrollbar").GetComponent<Scrollbar>().onValueChanged.RemoveAllListeners();
            foreach (Transform child in list.content.transform)
            {
                Destroy(child.gameObject);
            }
            list.gameObject.SetActive(false);
            skillPanel.gameObject.SetActive(false);

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
            _result = BattleLog.Result.Win;
            list.gameObject.SetActive(true);
        }

        private void DummyBattleLose()
        {
            _result = BattleLog.Result.Lose;
            list.gameObject.SetActive(true);
        }

        private void DummyBattle(int stageId)
        {
            Find<BattleResult>()?.Close();
            Find<Menu>()?.Close();
            Find<Menu>()?.ShowWorld();
            
            var simulator = new Simulator(new DebugRandom(), States.Instance.currentAvatarState.Value, new List<Consumable>(), stageId, _selectedSkill);
            simulator.Simulate();
            simulator.Log.result = _result;
            
            var stage = Game.Game.instance.stage;
            stage.Play(simulator.Log);
            
            Close();
        }

        private void DummySkill()
        {
            skillPanel.gameObject.SetActive(true);
        }

        private void SelectSkill(Game.Skill skill)
        {
            _selectedSkill = skill;
            DummyBattle(1);
        }
    }
}
