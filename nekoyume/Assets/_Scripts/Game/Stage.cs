using System.Collections;
using System.Linq;
using DG.Tweening;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Character;
using Nekoyume.Game.Entrance;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Trigger;
using Nekoyume.Model;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Stage : MonoBehaviour, IStage
    {
        private GameObject _background;
        public int Id;
        private BattleLog battleLog;

        public string BackgroundName
        {
            get
            {
                if (_background == null)
                    return "";

                return _background.name;
            }
        }

        private void Awake()
        {
            Event.OnRoomEnter.AddListener(OnRoomEnter);
            Event.OnPlayerDead.AddListener(OnPlayerDead);
            Event.OnPlayerDead.AddListener(OnPlayerSleep);
            Event.OnPlayerSleep.AddListener(OnPlayerSleep);
            Event.OnStageStart.AddListener(OnStageStart);
        }

        private void OnStageStart()
        {
            battleLog = ActionManager.Instance.battleLog;
            Play();
        }

        private void Start()
        {
            LoadBackground("nest");
        }

        private void OnRoomEnter()
        {
            gameObject.AddComponent<RoomEntering>();
        }

        private void OnPlayerDead()
        {
        }

        public void LoadBackground(string prefabName, float fadeTime = 0.0f)
        {
            if (_background != null)
            {
                if (_background.name.Equals(prefabName)) return;
                if (fadeTime > 0.0f)
                {
                    var sprites = _background.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var sprite in sprites)
                    {
                        sprite.sortingOrder += 1;
                        sprite.DOFade(0.0f, fadeTime);
                    }
                }

                Destroy(_background, fadeTime);
                _background = null;
            }

            var resName = $"Prefab/Background/{prefabName}";
            var prefab = Resources.Load<GameObject>(resName);
            if (prefab != null)
            {
                _background = Instantiate(prefab, transform);
                _background.name = prefabName;
            }
        }

        private void OnPlayerSleep()
        {
            StartCoroutine(SleepAsync());
        }

        private IEnumerator SleepAsync()
        {
            var tables = this.GetRootComponent<Tables>();
            Stats statsData;
            if (tables.Stats.TryGetValue(ActionManager.Instance.Avatar.Level, out statsData))
            {
                ActionManager.Instance.Sleep(statsData);
                while (ActionManager.Instance.Avatar.CurrentHP == ActionManager.Instance.Avatar.HPMax)
                    yield return new WaitForSeconds(1.0f);
            }

            OnRoomEnter();
        }

        public void Play()
        {
            if (battleLog?.Count > 0)
            {
                StartCoroutine(PlayAsync());
            }
        }

        private IEnumerator PlayAsync()
        {
            foreach (EventBase e in battleLog)
            {
                {
                    e.Execute(this);
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }

        public void StageEnter(int stage)
        {
            StartCoroutine(StageEnterAsync(stage));
        }

        private IEnumerator StageEnterAsync(int stage)
        {
            var roomPlayer = GetComponentInChildren<Character.Player>();
            if (roomPlayer != null)
            {
                roomPlayer.RunSpeed = 1.0f;
            }

            Data.Table.Stage data;
            var tables = this.GetRootComponent<Tables>();
            if (tables.Stage.TryGetValue(stage, out data))
            {
                var blind = Widget.Find<Blind>();
                yield return StartCoroutine(blind.FadeIn(1.0f, $"STAGE {stage}"));
                Widget.Find<Menu>().ShowWorld();

                LoadBackground(data.Background, 3.0f);

                yield return new WaitForSeconds(2.0f);
                yield return StartCoroutine(blind.FadeOut(1.0f));
            }
        }

        public void StageEnd(BattleResult.Result result)
        {
            StartCoroutine(StageEndAsync(result));
        }

        private IEnumerator StageEndAsync(BattleResult.Result result)
        {
            var blind = Widget.Find<Blind>();
            yield return blind.FadeIn(1.0f, result.ToString());
            yield return new WaitForSeconds(2.0f);
            Event.OnRoomEnter.Invoke();
        }

        public void SpawnPlayer()
        {
            var playerCharacter = GetComponentInChildren<Character.Player>();
            playerCharacter.RunSpeed = 1.2f;
            var player = playerCharacter.gameObject;
            playerCharacter.Init();
            var cam = Camera.main.gameObject.GetComponent<ActionCamera>();
            cam.target = player.transform;
        }

        public void SpawnMonster(Model.Monster monster)
        {
            var spawner = GetComponentsInChildren<MonsterSpawner>().First();
            spawner.SetData(Id, monster);
        }

        public void Dead(Model.CharacterBase character)
        {
            if (character is Model.Player)
            {
                var player = GetComponentInChildren<Character.Player>();
                player.Die();
            }
            else
            {
                var enemies = GetComponentsInChildren<Enemy>();
                var enemy = enemies.FirstOrDefault(e => e.id == character.id);
                if (enemy != null)
                {
                    enemy.Die();
                }
            }
        }

        public void Attack(int atk, Model.CharacterBase character, Model.CharacterBase target)
        {
            Character.CharacterBase attacker;
            Character.CharacterBase defender;
            var player = GetComponentInChildren<Character.Player>();
            var enemies = GetComponentsInChildren<Enemy>();
            if (character is Model.Player)
            {
                attacker = player;
                defender = enemies.FirstOrDefault(e => e.id == target.id);
            }
            else
            {
                attacker = enemies.FirstOrDefault(e => e.id == character.id);
                defender = player;
            }

            if (attacker != null && defender != null)
            {
                attacker.Attack(atk, defender);
            }
        }

        public void DropItem(Model.Monster character)
        {
            var enemies = GetComponentsInChildren<Enemy>();
            var enemy = enemies.FirstOrDefault(e => e.id == character.id);
            if (enemy != null)
            {
                enemy.DropItem(character.item);
            }
        }
    }
}
