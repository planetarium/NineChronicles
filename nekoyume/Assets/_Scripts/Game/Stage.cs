using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Entrance;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.Game.Trigger;
using Nekoyume.Game.Util;
using Nekoyume.Model;
using Nekoyume.UI;
using Nekoyume.Game.VFX;
using Nekoyume.Game.VFX.Skill;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Stage : MonoBehaviour, IStage
    {
        private const float SkillDelay = 0.1f;
        private const float StageStartPosition = -1.2f;
        
        public ObjectPool objectPool;
        public PlayerFactory playerFactory;
        public EnemyFactory enemyFactory;
        public DropItemFactory dropItemFactory;
        public SkillController SkillController;
        
        public MonsterSpawner spawner;
        
        public GameObject background;
        // dummy for stage background moving.
        public GameObject dummy;
        
        public int id;
        public Character.Player selectedPlayer;
        public readonly Vector2 questPreparationPosition = new Vector2(1.65f, -0.8f);
        public readonly Vector2 roomPosition = new Vector2(-2.66f, -1.85f);
        public bool repeatStage;
        public string zone;
        
        private Camera _camera;
        private BattleLog _battleLog;

        protected void Awake()
        {
            _camera = Camera.main;
            if (ReferenceEquals(_camera, null))
            {
                throw new NullReferenceException("`Camera.main` can't be null.");
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
            _battleLog = States.Instance.currentAvatarState.Value.battleLog;
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
            for (int i = 0; i < players.Length; ++i)
            {
                GameObject playerObject = players[i].gameObject;
                var anim = players[i].animator;
                if (index == i)
                {
                    playerObject.transform.DOScale(1.1f, 2.0f).SetDelay(0.2f);
                    playerObject.transform.DOMove(new Vector3(-1.25f, -0.7f), 2.4f).SetDelay(0.2f);
                    if (!ReferenceEquals(anim, null) && !anim.Target.activeSelf)
                    {
                        anim.Target.SetActive(true);
                        anim.Appear();
                    }

                    selectedPlayer = players[i];
                }
                else
                {
                    playerObject.transform.DOScale(0.9f, 1.0f);
                    playerObject.transform.DOMoveY(-3.6f, 2.0f);
                    if (!ReferenceEquals(anim, null) && anim.Target.activeSelf)
                    {
                        anim.Target.SetActive(true);
                        anim.Disappear();
                    }
                }
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
            if (background != null)
            {
                if (background.name.Equals(prefabName)) return;
                if (fadeTime > 0.0f)
                {
                    var sprites = background.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var sprite in sprites)
                    {
                        sprite.sortingOrder += 1;
                        sprite.DOFade(0.0f, fadeTime);
                    }

                    var particles = background.GetComponentsInChildren<ParticleSystem>();
                    foreach (var particle in particles)
                    {
                        particle.Stop();
                    }
                }

                Destroy(background, fadeTime);
                background = null;
            }

            var resName = $"Prefab/Background/{prefabName}";
            var prefab = Resources.Load<GameObject>(resName);
            if (ReferenceEquals(prefab, null)) return;
            background = Instantiate(prefab, transform);
            background.name = prefabName;
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
            yield return StartCoroutine(CoStageEnter(log.stage));
            foreach (EventBase e in log)
            {
                {
                    yield return StartCoroutine(e.CoExecute(this));
                }
            }
            yield return StartCoroutine(CoStageEnd(log.result));
        }

        private IEnumerator CoStageEnter(int stage)
        {
            if (Tables.instance.Background.TryGetValue(stage, out Data.Table.Background data))
            {
                id = stage;
                zone = data.background;
                LoadBackground(zone, 3.0f);
                RunPlayer();
                Widget.Find<Gold>().Close();

                var title = Widget.Find<StageTitle>();
                title.Show(stage);

                yield return new WaitForSeconds(2.0f);

                yield return StartCoroutine(title.CoClose());

                switch (zone)
                {
                    case "zone_0_0":
                        AudioController.instance.PlayMusic(AudioController.MusicCode.StageGreen);
                        break;
                    case "zone_0_1":
                        AudioController.instance.PlayMusic(AudioController.MusicCode.StageOrange);
                        break;
                    case "zone_0_2":
                        AudioController.instance.PlayMusic(AudioController.MusicCode.StageBlue);
                        break;
                }
            }
        }

        public IEnumerator CoStageEnd(BattleLog.Result result)
        {
            Widget.Find<BattleResult>().Show(result, repeatStage);
            if (result == BattleLog.Result.Win)
            {
                StartCoroutine(CoSlideBg());
            }
            else
            {
                objectPool.ReleaseAll();
            }

            yield return null;
        }

        private IEnumerator CoSlideBg()
        {
            RunPlayer();
            while (Widget.Find<BattleResult>().IsActive())
            {
                yield return new WaitForEndOfFrame();
            }
        }

        public IEnumerator CoSpawnPlayer(Model.Player character)
        {
            Widget.Find<Menu>().ShowWorld();

            var playerCharacter = RunPlayer();
            playerCharacter.Init(character);
            var player = playerCharacter.gameObject;

            var status = Widget.Find<Status>();
            status.UpdatePlayer(player);
            status.Show();
            status.ShowStage(id);

            ActionCamera.instance.ChaseX(player.transform);
            yield return null;
        }

        public IEnumerator CoAttack(CharacterBase caster, IEnumerable<Model.Skill.SkillInfo> skillInfos)
        {
            var character = GetCharacter(caster);
            var infos = skillInfos.ToList();

            yield return StartCoroutine(BeforeSkill(character, infos));

            yield return StartCoroutine(character.CoAttack(infos));

            yield return StartCoroutine(AfterSkill(character));
        }

        public IEnumerator CoAreaAttack(CharacterBase caster, IEnumerable<Model.Skill.SkillInfo> skillInfos)
        {
            var character = GetCharacter(caster);
            var infos = skillInfos.ToList();

            yield return StartCoroutine(BeforeSkill(character, infos));

            yield return StartCoroutine(character.CoAreaAttack(infos));

            yield return StartCoroutine(AfterSkill(character));
        }

        public IEnumerator CoDoubleAttack(CharacterBase caster, IEnumerable<Model.Skill.SkillInfo> skillInfos)
        {
            var character = GetCharacter(caster);
            var infos = skillInfos.ToList();

            yield return StartCoroutine(BeforeSkill(character, infos));

            yield return StartCoroutine(character.CoDoubleAttack(infos));

            yield return StartCoroutine(AfterSkill(character));
        }

        public IEnumerator CoHeal(CharacterBase caster, IEnumerable<Model.Skill.SkillInfo> skillInfos)
        {
            var character = GetCharacter(caster);
            var infos = skillInfos.ToList();

            yield return StartCoroutine(BeforeSkill(character, infos));

            yield return StartCoroutine(character.CoHeal(infos));

            yield return StartCoroutine(AfterSkill(character));
        }

        public IEnumerator CoDropBox(List<ItemBase> items)
        {
            if (items.Count > 0)
            {
                var dropItemFactory = GetComponent<DropItemFactory>();
                var player = GetPlayer();
                var position = player.transform.position;
                position.x += 1.0f;
                yield return StartCoroutine(dropItemFactory.CoCreate(items, position));
            }

            yield return null;
        }

        public IEnumerator CoGetReward(List<ItemBase> rewards)
        {
            foreach (var item in rewards)
            {
                Widget.Find<BattleResult>().Add(item);
            }

            yield return null;
        }

        public IEnumerator CoSpawnWave(List<Monster> monsters, bool isBoss)
        {
            var playerCharacter = GetPlayer();
            playerCharacter.StartRun();

            if (isBoss)
            {
                AudioController.instance.PlayMusic(AudioController.MusicCode.Boss1);
                VFXController.instance.Create<BattleBossTitleVFX>(Vector3.zero);
                StartCoroutine(Widget.Find<Blind>().FadeIn(0.4f, "", 0.2f));
                yield return new WaitForSeconds(2.0f);
                StartCoroutine(Widget.Find<Blind>().FadeOut(0.2f));
                yield return new WaitForSeconds(1.0f);
            }

            yield return StartCoroutine(spawner.CoSetData(id, monsters));

        }

        public IEnumerator CoGetExp(long exp)
        {
            var player = GetPlayer();
            yield return StartCoroutine(player.CoGetExp(exp));
        }

        public Character.Player GetPlayer()
        {
            var player = GetComponentInChildren<Character.Player>();
            if (ReferenceEquals(player, null))
            {
                var go = playerFactory.Create(States.Instance.currentAvatarState.Value);
                player = go.GetComponent<Character.Player>();

                if (ReferenceEquals(player, null))
                {
                    throw new NotFoundComponentException<Character.Player>();
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

        public Character.Player RunPlayer()
        {
            var player = GetPlayer();
            var playerTransform = player.transform;
            Vector2 position = playerTransform.position;
            position.y = StageStartPosition;
            playerTransform.position = position;
            player.StartRun();
            return player;
        }

        public Character.CharacterBase GetCharacter(CharacterBase caster) =>
            GetComponentsInChildren<Character.CharacterBase>().FirstOrDefault(c => c.Id == caster.id);

        private IEnumerator BeforeSkill(Character.CharacterBase character, IEnumerable<Model.Skill.SkillInfo> infos)
        {
            var enemy = GetComponentsInChildren<Character.CharacterBase>()
                .Where(c => c.gameObject.CompareTag(character.targetTag) && c.IsAlive())
                .OrderBy(c => c.transform.position.x).First();
            if (!character.TargetInRange(enemy))
                character.StartRun();

            yield return new WaitUntil(() => character.TargetInRange(enemy));
        }

        private IEnumerator AfterSkill(Character.CharacterBase character)
        {
            yield return new WaitForSeconds(SkillDelay);
            var enemy = GetComponentsInChildren<Character.CharacterBase>()
                .Where(c => c.gameObject.CompareTag(character.targetTag) && c.IsAlive())
                .OrderBy(c => c.transform.position.x).FirstOrDefault();
            if (enemy != null && !character.TargetInRange(enemy))
                character.StartRun();
        }
    }
}
