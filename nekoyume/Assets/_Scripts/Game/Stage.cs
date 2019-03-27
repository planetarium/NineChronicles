using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Game.Character;
using Nekoyume.Game.Entrance;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Trigger;
using Nekoyume.Game.Util;
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
        private const float AttackDelay = 0.3f;
        // dummy for stage background moving.
        [SerializeField]
        private GameObject dummy;
        public float loadingSpeed = 2.0f;
        private readonly Vector2 _stageStartPosition = new Vector2(-6.0f, -0.62f);
        public readonly Vector2 QuestPreparationPosition = new Vector2(1.8f, -0.4f);
        public readonly Vector2 RoomPosition = new Vector2(-2.4f, -1.3f);
        private bool _currentEventFinished;
        private PlayerFactory _factory;
        private MonsterSpawner _spawner;
        private Camera _camera;
        private ActionCamera _cam;
        private ObjectPool _objectPool;

        private void Awake()
        {
            _camera = Camera.main;
            if (ReferenceEquals(_camera, null))
            {
                throw new NullReferenceException("`Camera.main` can't be null.");
            }

            _cam = _camera.GetComponent<ActionCamera>();
            if (ReferenceEquals(_cam, null))
            {
                throw new NotFoundComponentException("Not found `ActionCamera`.");
            }

            _factory = GetComponent<PlayerFactory>();
            if (ReferenceEquals(_factory, null))
            {
                throw new NotFoundComponentException("Not found `PlayerFactory`.");
            }

            _objectPool = GetComponent<ObjectPool>();
            if (ReferenceEquals(_objectPool, null))
            {
                throw new NotFoundComponentException("Not found `ObjectPool`.");
            }

            _spawner = GetComponentInChildren<MonsterSpawner>();
            if (ReferenceEquals(_spawner, null))
            {
                throw new NotFoundComponentException("Not found `MonsterSpawner`.");
            }

            if (ReferenceEquals(dummy, null))
            {
                throw new NullReferenceException("`Dummy` can't be null.");
            }

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
            var players = GetComponentsInChildren<Character.Player>(true);
            foreach (var player in players)
            {
                _objectPool.Remove<Character.Player>(player.gameObject);
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
            if (!(ReferenceEquals(_background, null)))
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
            if (ReferenceEquals(prefab, null)) return;
            _background = Instantiate(prefab, transform);
            _background.name = prefabName;
        }

        public void Play(BattleLog log)
        {
            if (log?.Count > 0)
            {
                StartCoroutine(PlayAsync(log));
            }
        }

        private IEnumerator PlayAsync(BattleLog log)
        {
            GetPlayer(_stageStartPosition);
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
                var roomPlayer = ReadyPlayer();
                var blind = Widget.Find<Blind>();
                yield return StartCoroutine(blind.FadeIn(1.0f, $"STAGE {stage}"));

                LoadBackground(data.Background, 3.0f);
                Widget.Find<Menu>().ShowWorld();

                yield return new WaitForSeconds(1.5f);
                yield return StartCoroutine(blind.FadeOut(1.0f));
                _currentEventFinished = true;
                roomPlayer.StartRun();
            }
        }

        public void StageEnd(BattleLog.Result result)
        {
            Widget.Find<BattleResult>().Show(result);
            if (result == BattleLog.Result.Win)
            {
                StartCoroutine(CoSlideBg());
            }
            else
            {
                _objectPool.ReleaseAll();
            }
        }

        private IEnumerator CoSlideBg()
        {
            var roomPlayer = RunPlayer();
            dummy.transform.position = roomPlayer.transform.position;
            while (Widget.Find<BattleResult>().IsActive())
            {
                UpdateDummyPosition(roomPlayer, _cam);
                yield return new WaitForEndOfFrame();
            }
            _objectPool.ReleaseAll();
        }

        private void UpdateDummyPosition(Character.Player player, ActionCamera cam)
        {
            if (ReferenceEquals(cam, null)) throw new NotFoundComponentException("Not found `ActionCamera` component.");
            Vector2 position = dummy.transform.position;
            position.x += Time.deltaTime * player.Speed;
            dummy.transform.position = position;
            cam.target = dummy.transform;
        }

        public void SpawnPlayer(Model.Player character)
        {
            StartCoroutine(CoSpawnPlayer(character));
        }

        private IEnumerator CoSpawnPlayer(Model.Player character)
        {
            var playerCharacter = RunPlayer();
            playerCharacter.Init(character);
            var player = playerCharacter.gameObject;
            var status = Widget.Find<Status>();
            dummy.transform.position = new Vector2(0, 0);
            while (true)
            {
                var pos = _camera.ScreenToWorldPoint(status.transform.position);
                UpdateDummyPosition(playerCharacter, _cam);
                if (pos.x <= player.transform.position.x)
                {
                    playerCharacter.StartRun();
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
            _cam.target = player.transform;
            status.UpdatePlayer(player);
            _currentEventFinished = true;
        }

        public void SpawnMonster(Monster monster)
        {
            StartCoroutine(CoSpawnMonster(monster));
        }

        private IEnumerator CoSpawnMonster(Monster monster)
        {
            var playerCharacter = GetPlayer();
            playerCharacter.StartRun();
            _spawner.SetData(id, monster);
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
            var player = GetPlayer();
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
            yield return new WaitForSeconds(AttackDelay);
            _currentEventFinished = true;
        }

        public void DropBox(List<ItemBase> items)
        {
            throw new NotImplementedException();
        }

        public Character.Player GetPlayer()
        {
            var player = GetComponentInChildren<Character.Player>();
            if (ReferenceEquals(player, null))
            {
                var go = _factory.Create();
                player = go.GetComponent<Character.Player>();

                if (ReferenceEquals(player, null))
                {
                    throw new NotFoundComponentException("Not found `Character.Player` component.");
                }
            }

            return player;
        }

        public Character.Player GetPlayer(Vector2 position)
        {
            var player = GetPlayer();
            player.transform.position = position;
            return player;
        }

        private Character.Player RunPlayer()
        {
            var player = GetPlayer();
            var playerTransform = player.transform;
            var position = playerTransform.position;
            position.y = _stageStartPosition.y;
            playerTransform.position = position;
            player.StartRun();
            player.RunSpeed *= loadingSpeed;
            return player;
        }

        public Character.Player ReadyPlayer()
        {
            var player = GetPlayer(_stageStartPosition);
            player.RunSpeed = 0.0f;
            return player;
        }
    }
}
