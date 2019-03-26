using System.Collections;
using System.Linq;
using DG.Tweening;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Game.Character;
using Nekoyume.Game.Entrance;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Trigger;
using Nekoyume.Model;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Stage : MonoBehaviour, IStage
    {
        private GameObject _background;
        public int id;
        private BattleLog _battleLog;

        private void Awake()
        {
            Event.OnNestEnter.AddListener(OnNestEnter);
            Event.OnLoginDetail.AddListener(OnLoginDetail);
            Event.OnRoomEnter.AddListener(OnRoomEnter);
            Event.OnPlayerDead.AddListener(OnPlayerDead);
            Event.OnStageStart.AddListener(OnStageStart);
        }

        private void OnStageStart()
        {
            var qp = Widget.Find<QuestPreparation>();
            if (qp.IsActive())
            {
                qp.Close();
            }
            _battleLog = ActionManager.Instance.battleLog;
            Play(_battleLog);
        }

        private void Start()
        {
            OnNestEnter();
        }

        private void OnNestEnter()
        {
            gameObject.AddComponent<NestEntering>();
        }

        private void OnLoginDetail(int index)
        {
            var objectpool = GetComponent<Util.ObjectPool>();
            var players = GetComponentsInChildren<Character.Player>(true);
            foreach (var player in players)
            {
                objectpool.Remove<Character.Player>(player.gameObject);
            }
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

        public void Play(BattleLog log)
        {
            if (log?.Count > 0)
            {
                StartCoroutine(PlayAsync(log));
            }
        }

        private bool _currentEventFinished;

        private IEnumerator PlayAsync(BattleLog log)
        {
            var roomPlayer = GetComponentInChildren<Character.Player>();
            if (roomPlayer != null)
            {
                roomPlayer.RunSpeed = 0.0f;
                roomPlayer.gameObject.transform.position = new Vector2(-2.4f, -0.62f);
            }

            StageEnter(log.stage);
            foreach (EventBase e in log)
            {
                {
                    yield return new WaitUntil(() => _currentEventFinished);
                    if (!e.skip)
                    {
                        _currentEventFinished = false;
                        e.Execute(this);
                    }
                }
            }
            StageEnd(log.result);
        }

        public void StageEnter(int stage)
        {
            StartCoroutine(StageEnterAsync(stage));
        }

        private IEnumerator StageEnterAsync(int stage)
        {
            Data.Table.Stage data;
            var tables = this.GetRootComponent<Tables>();
            if (tables.Stage.TryGetValue(stage, out data))
            {
                var blind = Widget.Find<Blind>();
                yield return StartCoroutine(blind.FadeIn(1.0f, $"STAGE {stage}"));

                LoadBackground(data.Background, 3.0f);
                Widget.Find<Menu>().ShowWorld();

                yield return new WaitForSeconds(1.5f);
                yield return StartCoroutine(blind.FadeOut(1.0f));
                _currentEventFinished = true;
            }
        }

        public void StageEnd(BattleLog.Result result)
        {
            var objectPool = GetComponent<Util.ObjectPool>();
            objectPool.ReleaseAll();
            Widget.Find<BattleResult>().Show(result);
        }

        public void SpawnPlayer(Model.Player character)
        {
            StartCoroutine(CoSpawnPlayer(character));
        }

        private IEnumerator CoSpawnPlayer(Model.Player character)
        {
            var playerCharacter = GetComponentInChildren<Character.Player>();
            if (playerCharacter == null)
            {
                var factory = GetComponent<PlayerFactory>();
                playerCharacter = factory.Create().GetComponent<Character.Player>();
            }
            var player = playerCharacter.gameObject;
            playerCharacter.Init(character);
            playerCharacter.StartRun();
            var cam = Camera.main.gameObject.GetComponent<ActionCamera>();
            cam.target = player.transform;
            Widget.Find<Status>().UpdatePlayer(player);
            yield return null;
            _currentEventFinished = true;
        }

        public void SpawnMonster(Monster monster)
        {
            StartCoroutine(CoSpawnMonster(monster));
        }

        private IEnumerator CoSpawnMonster(Monster monster)
        {
            var playerCharacter = GetComponentInChildren<Character.Player>();
            if (playerCharacter == null)
            {
                var factory = GetComponent<PlayerFactory>();
                playerCharacter = factory.Create().GetComponent<Character.Player>();
            }
            playerCharacter.StartRun();
            var spawner = GetComponentsInChildren<MonsterSpawner>().First();
            spawner.SetData(id, monster);
            _currentEventFinished = true;
            yield return null;
        }

        public void Dead(Model.CharacterBase character)
        {
        }

        public void Attack(int atk, Model.CharacterBase character, Model.CharacterBase target, bool critical)
        {
            StartCoroutine(CoAttack(atk, character, target, critical));
        }

        private IEnumerator CoAttack(int atk, Model.CharacterBase character, Model.CharacterBase target, bool critical)
        {
            Character.CharacterBase attacker;
            Character.CharacterBase defender;
            var player = GetComponentInChildren<Character.Player>();
            var enemies = GetComponentsInChildren<Enemy>();
            if (character is Model.Player)
            {
                attacker = player;
                defender = enemies.FirstOrDefault(e => e.id == target.id);
                if (!player.TargetInRange(defender))
                {
                    attacker.StartRun();
                }

                yield return new WaitUntil(() => player.TargetInRange(defender));
            }
            else
            {
                attacker = enemies.FirstOrDefault(e => e.id == character.id);
                defender = player;
            }

            if (attacker != null && defender != null)
            {
                yield return StartCoroutine(attacker.CoAttack(atk, defender, critical));
            }

            yield return new WaitForSeconds(0.3f);
            _currentEventFinished = true;
        }

        public void DropItem(Monster character)
        {
        }
    }
}
